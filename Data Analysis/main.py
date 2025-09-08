import json
import math
import statistics
import os
import re
import pandas as pd
import statsmodels.formula.api as smf
from statsmodels.stats.anova import AnovaRM
import numpy as np
import matplotlib.pyplot as plt
from scipy.stats import ttest_rel

#Known constants
MIDI_TPQN = 960
DEFAULT_BPM = 120 #Used for pre-post tests
REFERENCES_PATH = "referenceRecordings/"
PARTICIPANTS_PATH = "."
PRETEST_FILENAME = "120BPM Combined - Pretest"
POSTTEST_FILENAME = "120BPM Combined - Final"

filewhitelist = ["120BPM Combined - Final.mid"] #Stop post-tests from appearing in Playthrough analysis

#Can be used to filter for specific notes for more detailed analysis, though when attempted models failed to converge likely due to small sample size.
KICK_NOTE = 36
SNARE_NOTE = 38
HAT_NOTE = 46

MIDIFile_Difficulties = { #Linear representation for each Component
    "60BPM Hat.mid" : 1,
    "60BPM Snare & Hat.mid": 2,
    "60BPM Kick & Hat.mid" : 3,
    "60BPM Combined.mid" : 4,
    "120BPM Combined.mid": 5,

}

Participant_Proficiency = { #Proficiency from questionnaire
    1: 0,
    2: 0,
    3: 1,
    4: 2,
    5: 0,
    6: 1,
    7: 4,
    8: 0
}

Participant_VR_Experience = { # VR Experience from questionnaire
    1:1,
    2:1,
    3:2,
    4:2,
    5:1,
    6:1,
    7:1,
    8:2
}

#Helper functions
def IOI(times): #Calculate differences between elements in a list for inter-onset intervals
    return np.diff(times)

def getFancyName(name):
    return re.sub(r'(?<=[A-Z])(?=[A-Z][a-z])', ' ', re.sub(r'(?<=[a-z])(?=[A-Z])', ' ', name)) #add spaces to variable names. e.g MeanIOIError becomes Mean IOI Error, for table captions etc.

def matchMidiFilename(filename):
    match = re.search(r"(\d+BPM .*?\.mid)",filename)
    if match:
        return match.group()
    else: return None

def tickToMs(tick,BPM):
    return (60000.0 / (BPM * MIDI_TPQN)) * tick

def getBPMfromFilename(filename): #BPM is determined by filename - in future the BPM should be stored in the JSON
    bpm = re.search("^(.*?)(?=BPM)",os.path.basename(filename))
    if bpm:
        return int(bpm.group())
    else: return None

def loadFromJsons(directory):
    loadedList = []
    with os.scandir(directory) as dir:
        for file in dir:
            if not file.is_file():
                continue
            with open(file,'r') as f:
                if not f.name.endswith('.json') or (any(bannedstring in f.name for bannedstring in filewhitelist)): #check for post-test file and skip if necessary
                    #print(f"Skipping non-json or whitelisted file {os.path.basename(f.name)}...")
                    continue
                    
                jsonFile = json.load(f)
                name = matchMidiFilename(os.path.basename(f.name))
                bpm = getBPMfromFilename(f.name)
                jsonFile['name'] = name if name is not None else "None"
                jsonFile['bpm'] = bpm if bpm is not None else "None"
                loadedList.append(jsonFile)
    return loadedList

def loadConditionData(conditionFolder):
    dataPath = conditionFolder+"/condition_data.json"
    with open(dataPath,'r') as f:
        jsonFile = json.load(f)
        return jsonFile

def groupSimultaneousNotes(hitTimes,window=30): #group notes within a 30ms window.

    if hitTimes:
        groups = []
        currentGroup = [hitTimes[0]] #create a group immediately
        for time in hitTimes[1:]: #for notes after current
            if abs(time - currentGroup[0]) <= window:
                currentGroup.append(time) #add if within 30ms
            else:
                groups.append(currentGroup) 
                currentGroup = [time] #otherwise create a new group
        groups.append(currentGroup)
        return list(map(statistics.mean,groups)) #return new list with each group averaged, combining the notes
    
    else: return []

## Metrics

def calculateTempoError(performanceIOIs,referenceIOIs):
    IOIErrors = []
    #for i in range(0,min(len(performanceIOIs),len(referenceIOIs))):
    for performance, reference in zip(performanceIOIs,referenceIOIs):
        if reference != 0:
            error = abs(performance - reference) / reference
            if error < 1: #don't include errors above 100% - presumed unintentional
                IOIErrors.append(error)
        else:
            IOIErrors.append(np.nan)
    return IOIErrors

def meanIOIError(hitTimes,referenceTimes):
    
    hitIOIs = IOI(groupSimultaneousNotes(hitTimes))
    referenceIOIs = IOI(groupSimultaneousNotes(referenceTimes))
    IOIErrors = calculateTempoError(hitIOIs,referenceIOIs)
    return statistics.mean(IOIErrors) * 100 if IOIErrors else np.nan

def meanAsynchrony(rawHits,rawRefs):
    hits, refs = matchHits(groupSimultaneousNotes(rawHits),groupSimultaneousNotes(rawRefs)) #group simultaneous hits, e.g played on different drums at the same time. match notes to closest reference
    asynchronies = [abs(h-c) for h,c in zip(hits,refs)] #abs(hit time - reference time)
    return statistics.mean(asynchronies) if asynchronies else np.nan

def meanRelativeAsynchrony(rawHits,rawRefs):
    hits, refs = matchHits(groupSimultaneousNotes(rawHits),groupSimultaneousNotes(rawRefs))
    IOIs = IOI(refs) #calculate inter-onset intervals for grouped reference notes
    asynchronies = [(h-c)/ioi for h,c,ioi in zip(hits,refs,IOIs)] #(hit time - reference time) / IOI
    return statistics.mean(asynchronies) * 100 if asynchronies else np.nan

def hitRate(frames,referenceTimes, bpm):
    successfulHitTimes = [tickToMs(frame['hitTime'],bpm) for frame in frames if frame['hitSuccessfully']]
    return (len(successfulHitTimes)/len(referenceTimes)) * 100 if len(referenceTimes) > 0 else np.nan

def errorRate(frames,referenceTimes,bpm):
    HitRate = hitRate(frames,referenceTimes,bpm)
    return 100 - HitRate if not np.isnan(HitRate) else np.nan

def excessHitRate(frames,referenceTimes,bpm):
    unsuccessfulHitTimes = [tickToMs(frame['hitTime'],bpm) for frame in frames if not frame['hitSuccessfully']]
    return (len(unsuccessfulHitTimes) / len(referenceTimes) * 100) if len(referenceTimes) > 0 else np.nan

def matchHits(hits,referenceHits):
    #slow but functional. iterate through notes and pair them to their closest reference, removing any extraneous ones
    hitWindow = 300
    toReturn = []
    for ref in referenceHits:
        currentMatch = None
        currentIdx = None
        currentDiff = None
        for i,hit in enumerate(hits):
            diff = abs(hit-ref)
            if diff <= hitWindow and (currentMatch is None or diff < currentDiff):
                currentMatch = hit
                currentIdx = i
                currentDiff = diff
        if currentMatch is not None:
            toReturn.append((currentMatch,ref))
            hits.pop(currentIdx)

    if toReturn:
        hits,referenceHits = zip(*toReturn)
        return list(hits),list(referenceHits)
    else: return [],[]

results = []
PrePostTestResults = []
referenceRecordings = loadFromJsons(REFERENCES_PATH)

for participantFolder in (item for item in os.scandir(PARTICIPANTS_PATH) if os.path.isdir(item)):
    PreTests = []
    PostTests = []
    search = re.search('Participant (\d+)',participantFolder.name)
    if search:
        ptpID = int(search.group(1))
        print(f"Loading participant {ptpID}...")

        for PretestFile in (item for item in os.scandir(participantFolder) if item.is_file()):
            if PRETEST_FILENAME in PretestFile.name:
                with open (PretestFile) as f:
                    PreTests.append({'Participant':ptpID,'File':json.load(f)})
        
        current = next((PreTest for PreTest in PreTests if PreTest['Participant']==ptpID),None)
        if current:
            currentPreTest = current['File']
        else:
            continue

    else:
        print("Invalid participant folder name! Skipping")
        continue

    for conditionFolder in (item for item in os.scandir(participantFolder) if os.path.isdir(item)):

        condition = re.search('Condition\s+([A-D])',conditionFolder.name)
        if condition:
            conditionID = condition.group(1)
            print(f"Loading condition {conditionID}...")
            conditionData = loadConditionData(conditionFolder.path)
        else:
            print(f"Invalid condition folder name: {conditionFolder.name}! Skipping")
            continue

        
        for PostTestFile in (item for item in os.scandir(conditionFolder) if os.path.isfile(item) and POSTTEST_FILENAME in item.name):
            with open(PostTestFile) as f:
                PostTest = json.load(f)
                PostTests.append( {'Position' : conditionData['Position'],'File': PostTest} )
            
        PostTests = sorted(PostTests, key=lambda x: x['Position']) #Sort post-tests by ordinal position
        if not PostTests:
            print(f"No Post test for participant {ptpID}")
            continue
        currentPostTest = PostTests[0]['File']


        #------------------Pre/Post Data comparison here --------------------
        # Default BPM of 120 is used here as this is what was used for all pre-post tests.
        reference = next((ref for ref in referenceRecordings if "120BPM Combined" in ref["name"] ),None)
        if reference:
            referenceTimes = [tickToMs(frame['hitTime'],DEFAULT_BPM) for frame in reference['frames']]
        else:
            referenceTimes = []

        PreTestFrames = currentPreTest['frames']
        PostTestFrames = currentPostTest['frames']

        PreHitTimes = [tickToMs(frame['hitTime'],DEFAULT_BPM) for frame in PreTestFrames]
        PostHitTimes = [tickToMs(frame['hitTime'],DEFAULT_BPM) for frame in PostTestFrames]

        PreTestErrorRate = errorRate(PreTestFrames,referenceTimes,DEFAULT_BPM)
        PostTestErrorRate = errorRate(PostTestFrames,referenceTimes,DEFAULT_BPM)

        PreTestExcessHitRate = excessHitRate(PreTestFrames,referenceTimes,DEFAULT_BPM)
        PostTestExcessHitRate = excessHitRate(PostTestFrames,referenceTimes,DEFAULT_BPM)
        

        PreMeanAsynchrony = meanAsynchrony(PreHitTimes,referenceTimes)
        PostMeanAsynchrony = meanAsynchrony(PostHitTimes,referenceTimes)

        PreMeanRelativeAsynchrony = meanRelativeAsynchrony(PreHitTimes,referenceTimes)
        PostMeanRelativeAsynchrony = meanRelativeAsynchrony(PostHitTimes,referenceTimes)

        PreMeanIOIError = meanIOIError(PreHitTimes,referenceTimes)
        PostMeanIOIError = meanIOIError(PostHitTimes,referenceTimes)


        MeanAsynchronyDiff = PostMeanAsynchrony - PreMeanAsynchrony
        MeanIOIErrorDiff = PostMeanIOIError - PreMeanIOIError
        MeanRelativeAsynchronyDiff = PostMeanRelativeAsynchrony - PreMeanRelativeAsynchrony
        errorRateDiff = PostTestErrorRate - PreTestErrorRate
        excessHitRateDiff = PostTestExcessHitRate - PreTestExcessHitRate


        PrePostTestResults.append({
            'Participant ID':ptpID,
            'Condition':conditionID,

            'PreErrorRate':PreTestErrorRate,
            'PreExcessHitRate':PreTestExcessHitRate,

            'PostErrorRate':PostTestErrorRate,
            'PostExcessHitRate':PostTestExcessHitRate,

            'excessHitRateDelta':excessHitRateDiff,
            'errorRateDelta':errorRateDiff,
            'PreMeanAsynchrony':PreMeanAsynchrony,
            'PreMeanRelativeAsynchrony':PreMeanRelativeAsynchrony,
            'PreMeanIOIError':PreMeanIOIError,
            'PostMeanAsynchrony':PostMeanAsynchrony,
            'PostMeanRelativeAsynchrony':PostMeanRelativeAsynchrony,
            'PostMeanIOIError':PostMeanIOIError,
            'MeanAsynchronyDelta':MeanAsynchronyDiff,
            'MeanRelativeAsynchronyDelta':MeanRelativeAsynchronyDiff,
            'MeanIOIErrorDelta':MeanIOIErrorDiff
        })

        currentPreTest = currentPostTest #Previous post-test becomes pre-test
        PostTests.pop()

        for performanceFolder in (item for item in os.scandir(conditionFolder) if os.path.isdir(item)):
            
            performances = loadFromJsons(performanceFolder)
            for performance in performances:
                MIDIFile = matchMidiFilename(performance['name'])
                reference = next((ref for ref in referenceRecordings if ref['name'] == MIDIFile),None)

                BPM = performance['bpm']
                frames = performance['frames']
                referenceFrames = reference['frames']
                hitTimes = [tickToMs(frame['hitTime'],BPM) for frame in frames]
                referenceBPM = reference['bpm']
                referenceTimes = [tickToMs(frame['hitTime'],referenceBPM) for frame in referenceFrames]

                if BPM != referenceBPM:
                    print("WARNING: BPMs do not match!")

                MeanAsynchrony = meanAsynchrony(hitTimes,referenceTimes)
                MeanRelativeAsynchrony = meanRelativeAsynchrony(hitTimes,referenceTimes)
                MeanIOIError = meanIOIError(hitTimes,referenceTimes)
                ErrorRate = errorRate(frames,referenceTimes,BPM)
                ExcessHitRate = excessHitRate(frames,referenceTimes,BPM)

                results.append({
                    'Participant ID': ptpID,
                    'Proficiency': Participant_Proficiency.get(ptpID,0),
                    'VRExperience':Participant_VR_Experience.get(ptpID),
                    'Condition': conditionID,
                    'Order': conditionData['Position'],
                    'ErrorRate': ErrorRate,
                    'ExcessHitRate': ExcessHitRate,
                    'CognitiveLoad': conditionData['Paas'],
                    'Difficulty':MIDIFile_Difficulties.get(MIDIFile,0),
                    'MIDIFile': MIDIFile,
                    'MeanAsynchrony' : MeanAsynchrony,
                    'MeanRelativeAsynchrony' : MeanRelativeAsynchrony,
                    'MeanIOIError' : MeanIOIError,
                    'BPM':BPM,
                    })

resultsDataFrame = pd.DataFrame(results)
resultsDataFrame = resultsDataFrame.dropna() #remove any missing data

#centre difficulty and cognitive load, making intercept the average
resultsDataFrame["Component"] = resultsDataFrame["Difficulty"] - resultsDataFrame["Difficulty"].mean()
resultsDataFrame["CognitiveLoad"] = resultsDataFrame["CognitiveLoad"] - resultsDataFrame["CognitiveLoad"].mean()

models = []

modelsFormulas = [{"name": "MeanAsynchrony",'dependentVariable':'MeanAsynchrony',"formula":"MeanAsynchrony ~ Condition + Component + CognitiveLoad"},
                  {"name": "MeanRelativeAsynchrony",'dependentVariable':'MeanRelativeAsynchrony',"formula":"MeanRelativeAsynchrony ~ Condition + Component + CognitiveLoad"},
                  {"name": "MeanIOIError",'dependentVariable':'MeanIOIError',"formula":"MeanIOIError ~ Condition + Component + CognitiveLoad"},
                  {"name":"ErrorRate",'dependentVariable':'ErrorRate',"formula":"ErrorRate ~ Condition + Component + CognitiveLoad"},
                  {"name":"ExcessHitRate",'dependentVariable':'ExcessHitRate',"formula":"ExcessHitRate ~ Condition + Component + CognitiveLoad"},

                  #Going in Appendix
                  {"name": "MeanAsynchrony (B Baseline)",'dependentVariable':'MeanAsynchrony', "formula": "MeanAsynchrony ~ C(Condition, Treatment(reference='B')) + Component + CognitiveLoad"},
                  {"name": "MeanRelativeAsynchrony (B Baseline)",'dependentVariable':'MeanRelativeAsynchrony', "formula": "MeanRelativeAsynchrony ~ C(Condition, Treatment(reference='B')) + Component + CognitiveLoad"},
                  {"name": "MeanIOIError (B Baseline)",'dependentVariable':'MeanIOIError', "formula": "MeanIOIError ~ C(Condition, Treatment(reference='B')) + Component + CognitiveLoad"},
                  {"name":"ErrorRate (B Baseline)",'dependentVariable':'ErrorRate', "formula":"ErrorRate ~ C(Condition, Treatment(reference='B')) + Component + CognitiveLoad"},
                  {"name":"ExcessHitRate (B Baseline)",'dependentVariable':'ExcessHitRate',"formula":"ExcessHitRate ~ C(Condition, Treatment(reference='B')) + Component + CognitiveLoad"},
                  
                  {"name": "MeanAsynchrony (C Baseline)",'dependentVariable':'MeanAsynchrony', "formula": "MeanAsynchrony ~ C(Condition, Treatment(reference='C')) + Component + CognitiveLoad"},
                  {"name": "MeanRelativeAsynchrony (C Baseline)",'dependentVariable':'MeanRelativeAsynchrony', "formula": "MeanRelativeAsynchrony ~ C(Condition, Treatment(reference='C')) + Component + CognitiveLoad"},
                  {"name": "MeanIOIError (C Baseline)",'dependentVariable':'MeanIOIError', "formula": "MeanIOIError ~ C(Condition, Treatment(reference='C')) + Component + CognitiveLoad"},
                  {"name":"ErrorRate (C Baseline)",'dependentVariable':'ErrorRate', "formula":"ErrorRate ~ C(Condition, Treatment(reference='B')) + Component + CognitiveLoad"},
                  {"name":"ExcessHitRate (C Baseline)",'dependentVariable':'ExcessHitRate',"formula":"ExcessHitRate ~ C(Condition, Treatment(reference='C')) + Component + CognitiveLoad"},

                  {"name": "CognitiveLoad",'dependentVariable':'CognitiveLoad',"formula":"CognitiveLoad ~ Condition"}
                  ]


conditions = ['A','B','C','D']
for modelInfo in modelsFormulas:
    model = smf.mixedlm(modelInfo["formula"], data=resultsDataFrame, groups=resultsDataFrame["Participant ID"],re_formula='1')
    result = model.fit()

    raw = resultsDataFrame.groupby("Condition")[modelInfo["dependentVariable"]].agg(['mean','std']).round(2) #raw mean and standard deviation no longer used in favour of pred. m and se

    rows = []
    intercept = result.params.get("Intercept", 0)
    for param in result.params.index:

        coefficient = round(result.params[param],2)
        coefficientString = str(coefficient)
        SE = str(round(result.bse[param],2))
        pvalue = result.pvalues[param]
        rounded = str(round(pvalue,3))
        if pvalue < 0.001:
            pvalueString = "<.001*"
        else: pvalueString = rounded+"*" if pvalue < 0.05 else rounded
        z = str(round(result.tvalues[param],2))
        
        PredictedMean = str(round(intercept + result.params[param], 2))
        
        mean = '-'
        std = '-'
        if param == 'Intercept':
            currentCondition = 'A'
            pvalueString = '-'
            z = '-'
            PredictedMean = coefficientString

        else:
            paramToList = list(param)
            paramToList.reverse()
            currentCondition = paramToList[1]

        if currentCondition in conditions:
            mean = str(raw.loc[currentCondition,'mean'])
            std = str(raw.loc[currentCondition,'std'])
        else:
            PredictedMean = '-'

        meanstd = '-'
        pmeanse = '-'
        if mean != '-':
            meanstd = f"{mean} ± {std}"
        if PredictedMean != '-':
            pmeanse = f"{PredictedMean} ± {SE}" 

        rows.append([param,coefficientString,pmeanse,pvalueString])
        df = pd.DataFrame(rows, columns = ["Predictor","Estimate","Pred. M ± SE","p-value"])
        df["Predictor"] = df["Predictor"].replace({
            "Intercept":"Baseline (A)",
            "Condition[T.A]":"Condition A",
            "Condition[T.B]":"Condition B",
            "Condition[T.C]":"Condition C",
            "Condition[T.D]":"Condition D",
            "C(Condition, Treatment(reference='B'))[T.A]":"Condition A",
            "C(Condition, Treatment(reference='B'))[T.C]":"Condition C",
            "C(Condition, Treatment(reference='B'))[T.D]":"Condition D",
            "CognitiveLoad":"Cognitive Load"
        })
    
    fancyName = getFancyName(modelInfo['name'])
    
    csv = df.to_csv(index=False)
    with open(fancyName+" LMM results.csv",'w') as f:
        f.write(csv)

    latex = df.to_latex(index=False,column_format="lccccccc")

    latexTable = f"""
    \\begin{{table}}[H]
    \\centering
    \\begin{{small}}
    {latex}
    \\end{{small}}
    \\caption[Linear mixed-effects model results for {fancyName}]{{Linear mixed-effects model results for {fancyName}. \\newline * indicates statistical significance.}}
    \\label{{tab:{modelInfo['name'].lower()}}}

    \\end{{table}}"""

    with open(modelInfo['name']+".tex","w",encoding="utf-8") as f:
        f.write(latexTable)
    print(result.summary())

resultsDataFrame.to_csv("raw_metrics.csv",index=False)



PrePostDataFrame = pd.DataFrame(PrePostTestResults)
PrePostDataFrame.to_csv("raw_prepost.csv",index=False)
PrePostResults = []
for condition in PrePostDataFrame['Condition'].unique():
    conditionsDataFrame = PrePostDataFrame[PrePostDataFrame['Condition'] == condition]
    conditionsDataFrame = conditionsDataFrame.dropna()

    tExtraRate, pExtraRate = ttest_rel(conditionsDataFrame['PostExcessHitRate'],conditionsDataFrame['PreExcessHitRate'])
    tErrorRate, pErrorRate = ttest_rel(conditionsDataFrame['PostErrorRate'],conditionsDataFrame['PreErrorRate'])
    tAsync, pAsync = ttest_rel(conditionsDataFrame['PostMeanAsynchrony'], conditionsDataFrame['PreMeanAsynchrony'])
    tRasync, pRasync = ttest_rel(conditionsDataFrame['PostMeanRelativeAsynchrony'], conditionsDataFrame['PreMeanRelativeAsynchrony'])
    tIOIError,pIOIError = ttest_rel(conditionsDataFrame['PostMeanIOIError'],conditionsDataFrame['PreMeanIOIError'])
    

    PreError = conditionsDataFrame['PreErrorRate'].mean()
    PostError = conditionsDataFrame['PostErrorRate'].mean()
    PreExcess = conditionsDataFrame['PreExcessHitRate'].mean()
    PostExcess = conditionsDataFrame['PostExcessHitRate'].mean()
    PreAsync = conditionsDataFrame['PreMeanAsynchrony'].mean()
    PostAsync = conditionsDataFrame['PostMeanAsynchrony'].mean()
    PreRasync = conditionsDataFrame['PreMeanRelativeAsynchrony'].mean()
    PostRasync = conditionsDataFrame['PostMeanRelativeAsynchrony'].mean()
    PreIOIError = conditionsDataFrame['PreMeanIOIError'].mean()
    PostIOIError = conditionsDataFrame['PostMeanIOIError'].mean()

    #square root of sample size to calculate Cohen's d effect size (Cohen, 2013)
    rootSampleSize = math.sqrt(len(conditionsDataFrame))

    PrePostResults.append({
        'Condition':condition,
        'tErrorRate':tErrorRate,
        'pErrorRate':pErrorRate,
        'dErrorRate':tErrorRate / rootSampleSize,
        'tExcessHitRate':tExtraRate,
        'pExcessHitRate':pExtraRate,
        'dExcessHitRate':tExtraRate / rootSampleSize,
        'tAsync':tAsync,
        'pAsync':pAsync,
        'dAsync':tAsync / rootSampleSize,
        'tRasync':tRasync,
        'pRasync':pRasync,
        'dRasync':tRasync / rootSampleSize,
        'pIOIError':pIOIError,
        'tIOIError':tIOIError,
        'dIOIError':tIOIError / rootSampleSize,

        'deltaErrorRate': PostError - PreError,
        'deltaExcessHitRate': PostExcess - PreExcess,
        'deltaMeanAsynchrony': PostAsync - PreAsync,
        'deltaMeanRelativeAsynchrony': PostRasync - PreRasync,
        'deltaMeanIOIError': PostIOIError - PreIOIError,
        'preRasync': PreRasync,
        'postRasync': PostRasync

        })


PrePostResultsDataFrame = pd.DataFrame(PrePostResults)
PrePostResultsDataFrame.to_csv("prepost-results.csv")


metrics = [
    ('Error Rate', 'deltaErrorRate', 'tErrorRate', 'dErrorRate', 'pErrorRate'),
    ('Excess Hit Rate', 'deltaExcessHitRate', 'tExcessHitRate', 'dExcessHitRate', 'pExcessHitRate'),
    ('Mean Asynchrony', 'deltaMeanAsynchrony', 'tAsync', 'dAsync', 'pAsync'),
    ('Mean Relative Asynchrony', 'deltaMeanRelativeAsynchrony', 'tRasync', 'dRasync', 'pRasync'),
    ('Mean IOI Error','deltaMeanIOIError','tIOIError','dIOIError','pIOIError')
]

for condition in PrePostResultsDataFrame['Condition'].unique():
    # .squeeze() to give us one indexable row
    conditionData = PrePostResultsDataFrame[PrePostResultsDataFrame['Condition'] == condition].squeeze()

    rows = []
    for name, delta, t, d, p in metrics:
        deltaVal = str(round(conditionData[delta],3))
        tVal = str(round(conditionData[t],3))
        realP = conditionData[p]
        if realP < 0.05:
            if realP < 0.001:
                pVal = '<0.001*'
            else:
                pVal = str(round(realP,3))+'*'
        else:
            pVal = str(round(conditionData[p],3))
        dVal = str(round(conditionData[d],3))
        
        rows.append([name, deltaVal, tVal, dVal, pVal])
    
    df_table = pd.DataFrame(rows, columns=['Metric', 'Delta', 't', 'd', 'p'])

    latex = df_table.to_latex(index=False,column_format="lcccc")

    latexTable = f"""
    \\begin{{table}}[H]
    \\centering
    \\begin{{small}}
    {latex}
    \\end{{small}}
    \\caption[Pre-post t-test results for Condition {condition}]{{Pre-post t-test results for Condition {condition}. \\newline * indicates statistical significance.}}
    \\label{{tab:PrePost_{condition}}}
    \\end{{table}}"""

    with open(f"PrePost_{condition}.tex", "w",encoding='utf8') as f:
        f.write(latexTable)




######################### for latex plots

plt.rcParams.update({
    "text.usetex": True,
    "font.family": "serif",
    "mathtext.default": "regular", 
    "axes.formatter.use_mathtext": False,
    "font.size":10 #smaller size for the box plots
})

################################################################### Pre-post box plots
#Gemini was used to assist with the creation of all plots.



variables = ["ErrorRate","ExcessHitRate","MeanAsynchrony","MeanRelativeAsynchrony","MeanIOIError"]
conditions = PrePostDataFrame['Condition'].unique()
x = np.arange(len(conditions))
width = 0.35
outlierStyle = {'marker':'+','color':'purple','alpha':0.3}
for var in variables:
    preData = [PrePostDataFrame.loc[PrePostDataFrame['Condition']==c, "Pre"+var].values for c in conditions]
    postData = [PrePostDataFrame.loc[PrePostDataFrame['Condition']==c, "Post"+var].values for c in conditions]
    
    plt.figure(figsize=(3,2.5))
    preBoxPlot = plt.boxplot(preData, positions=x - width/2, widths=0.3, patch_artist=True, boxprops={'facecolor':'skyblue', 'alpha':0.5}, medianprops={'color':'blue'}, flierprops=outlierStyle)
    postBoxPlot = plt.boxplot(postData, positions=x + width/2, widths=0.3, patch_artist=True, boxprops={'facecolor':'salmon', 'alpha':0.5}, medianprops={'color':'red'}, flierprops=outlierStyle)
    
    plt.xticks(x, conditions)
    plt.xlabel("Condition")
    plt.ylabel(var)
    plt.grid(axis='y', alpha=0.3)
    plt.legend([preBoxPlot["boxes"][0], postBoxPlot["boxes"][0]], ["Pre", "Post"],fontsize=10,loc="lower center",bbox_to_anchor=(0.5,1.00),ncol=2,frameon=False)
    plt.tight_layout()
    plt.savefig(f"Pre-Post Boxplot-{var}.pgf")
    #plt.show()

############################################################################################## Violin Plots ########################################################################
#Gemini was used as a starting point here but then heavily adapted for consistency with other plots
plt.rcParams.update({'font.size':14})
colours = ['lightgreen', 'skyblue', 'salmon', 'plum']
for var in variables:
    groups = resultsDataFrame.groupby("Condition")[var].apply(list)

    plt.figure(figsize=(6,3.5))
    parts = plt.violinplot(groups.values,showmeans=True,showmedians=True)

    for part, colour in zip(parts['bodies'],colours):
        part.set_facecolor(colour)
        part.set_edgecolor('white')
        part.set_alpha(0.7)
    #Red dotted line for mean
    if parts['cmeans'] is not None:
        parts['cmeans'].set_color("red")
        parts['cmeans'].set_linestyle("--")
        parts['cmeans'].set_linewidth(1.5)
    #Plot raw points
    for i,condition in enumerate(groups.index):
        y = groups[condition]
        x = np.random.normal(i+1,0.08,size=len(y))
        plt.scatter(x,y,color='r',alpha=0.1)
    
    plt.ylabel(getFancyName(var))
    plt.xlabel("Condition")
    plt.yticks(ticks=np.arange(0,101,20))
    plt.xticks(ticks=np.arange(1, 5), labels=['A','B','C','D'])
    plt.grid(axis='both',linestyle='--',alpha=0.5)
    plt.subplots_adjust(bottom=0.2)
    plt.savefig(f"{var}-violin.pgf")
    plt.savefig(f"{var}-violin.png")