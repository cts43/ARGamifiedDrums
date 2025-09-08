import pandas as pd
import statsmodels.formula.api as smf
import scipy.stats as stats
import seaborn as sns
import matplotlib.pyplot as plt

questionnaireDataFrame = pd.read_csv("AR Drums Study_ Questionnaire (Responses) - Sheet1.csv")
questionnaireDataFrame = questionnaireDataFrame[questionnaireDataFrame['Aspect'] != 'Score']

Differentiation = questionnaireDataFrame[questionnaireDataFrame['Aspect'] == 'Differentiation']
Timing = questionnaireDataFrame[questionnaireDataFrame['Aspect'] == 'Timing']

Differentiation['DemonstrationType'] = Differentiation['DemonstrationType'].astype('category')
Timing['DemonstrationType'] = Timing['DemonstrationType'].astype('category')

Differentiation['DemonstrationType'] = Differentiation['DemonstrationType'].cat.reorder_categories(
    ['Video', 'Action Observation', 'Falling Notes'], ordered=True
)
Timing['DemonstrationType'] = Timing['DemonstrationType'].cat.reorder_categories(
    ['Video', 'Action Observation', 'Falling Notes'], ordered=True
)

aspectsDatas = {"Differentiation":Differentiation,"Timing":Timing}

for aspectName, aspectData in aspectsDatas.items():

    model = smf.mixedlm("Response ~ DemonstrationType", data=aspectData, groups=aspectData["Participant ID"])

    result = model.fit()
    print(result.summary())
    raw = aspectData.groupby("DemonstrationType")['Response'].agg(['mean','std']).round(2) #raw mean and standard deviation no longer used in favour of pred. m and se

    conditions = ['Video','Falling Notes','Action Observation']
    rows = []
    intercept = result.params.get("Intercept", 0)
    for param in result.params.index:

        coefficient = round(result.params[param],2)
        coefficientString = str(coefficient)
        SE = str(round(result.bse[param],2))
        pvalue = result.pvalues[param]
        rounded = str(round(pvalue,3))
        #show significance
        if pvalue < 0.001 and param != "Intercept": 
            pvalueString = "<.001*"
        else: pvalueString = rounded+"*" if pvalue < 0.05 and param != "Intercept" else rounded 
        z = str(round(result.tvalues[param],2))

        predictedMean = str(round(intercept + result.params[param], 2))

        currentCondition = ' '
        mean = '-'
        std = '-'
        if param == 'Intercept':
            currentCondition = "Video"     
            pvalueString = '-'
            z = '-'
            predictedMean = coefficientString
    
        else:
            try:
                paramToList = list(param)
                paramToList = paramToList[20:]
                paramToList.pop()
                currentCondition = ''.join(paramToList)
            except:
                pass
    
        if currentCondition in conditions:
            mean = str(raw.loc[currentCondition,'mean'])
            std = str(raw.loc[currentCondition,'std'])
        
        pmeanse = '-'
        if predictedMean != '-':
                pmeanse = f"{predictedMean} ± {SE}" 

        rows.append([param,coefficientString,pmeanse,pvalueString])
    

    df = pd.DataFrame(rows, columns = ["Predictor","Estimate","Pred. M ± SE","p-value"])
    df["Predictor"] = df["Predictor"].replace({
            "Intercept":"Video (Baseline)",
            "DemonstrationType[T.Action Observation]":"Action Observation",
            "DemonstrationType[T.Falling Notes]":"Falling Notes"
        })

    latex = df.to_latex(index=False,column_format="lccccccc")

    latexTable = f"""
    \\begin{{table}}[H]
    \\centering
    \\begin{{small}}
    {latex}
    \\end{{small}}
    \\caption[Linear mixed-effects model results for questionnaire: Note {aspectName}]{{LMM results for questionnaire: {aspectName}. \\newline * indicates statistical significance.}}
    \\label{{tab:differentiation}}
    \\end{{table}}"""

    with open(f"{aspectName}LMM.tex","w",encoding="utf-8") as f:
        f.write(latexTable)


############### latex formatting for plots ############

plt.rcParams.update({
    "text.usetex": True,
    "font.family": "serif",
    "font.size": 12,
    "mathtext.default": "regular", 
    "axes.formatter.use_mathtext": False
})


################################################################## Adapted from Gemini:


demo_order = ['Video', 'Action Observation', 'Falling Notes']
aspects = questionnaireDataFrame['Aspect'].unique()
n_aspects = len(aspects)
n_demos = len(demo_order)
# Prepare data for violinplot
data_to_plot = []
positions = []
bar_width = 0.2  # spacing between violins
for i, aspect in enumerate(aspects):
    for j, demo in enumerate(demo_order):
        subset = questionnaireDataFrame[
            (questionnaireDataFrame['Aspect'] == aspect) &
            (questionnaireDataFrame['DemonstrationType'] == demo)
        ]['Response'].values
        data_to_plot.append(subset)
        positions.append(i + j*bar_width)
# Create figure
plt.figure(figsize=(8,6))
parts = plt.violinplot(
    data_to_plot,
    positions=positions,
    widths=bar_width*0.9,
    showmeans=True,
    showmedians=True
)
demos = 3
colours = ['skyblue', 'lightgreen', 'salmon']
for i, patch in enumerate(parts['bodies']):
    demo = i % demos
    patch.set_facecolor(colours[demo])
    patch.set_alpha(0.7)
# Means
if parts['cmeans'] is not None:
    parts['cmeans'].set_color("red")
    parts['cmeans'].set_linestyle("--")
    parts['cmeans'].set_linewidth(1.5)
# X-axis labels at center of each Aspect group
plt.xticks(
    [i + bar_width*(n_demos-1)/2 for i in range(n_aspects)],
    aspects
)
plt.xlabel("Aspect")
plt.ylabel("Response")
# Add legend with dummy patches
for i, demo in enumerate(demo_order):
    plt.bar(0, 0, color=colours[i], label=demo)
plt.legend(title="Demonstration Type",fontsize=10)
plt.grid(axis='both', linestyle='--', alpha=0.5)
plt.tight_layout()
plt.savefig("response_violin.pgf")



###################### Participant Background (musical proficiency, VR experience etc)


plt.rcParams.update({
    "axes.edgecolor": "black",
    "axes.linewidth": 1.2,
    "grid.alpha": 0.3,
    "grid.linestyle": "--",
    "grid.linewidth": 0.7,
    "xtick.direction": "out",
    "ytick.direction": "out",
    "font.size": 10
})

boxStyle = {
    "boxprops": {"facecolor": "skyblue", "alpha": 0.5, "edgecolor": "white", "linewidth": 1.4},
    "medianprops": {"color": "blue", "linewidth": 1.2},
    "flierprops": {"marker": "+", "color": "purple", "alpha": 0.6},
    "whiskerprops": {"color": "black", "linewidth": 1.4},
    "capprops": {"color": "black", "linewidth": 1.4},
}

bgDf = pd.read_csv("AR Drums Study_ Questionnaire (Responses) - Ptp Background.csv")
instrumentDf = pd.read_csv("AR Drums Study_ Questionnaire (Responses) - Ptp Instruments.csv")

likertQs = ["(8a) Drum Proficiency","(8b) Drum Confidence","(11) Musical Timing Familiarity","(12) VR Experience","(13) Rhythm Game Experience"]
bgDfLikert = bgDf.melt(id_vars="Participant ID",var_name="Question",value_vars=likertQs,value_name="Response")

bgDfLikert["Response"] = pd.to_numeric(bgDfLikert["Response"], errors="coerce")

plt.figure(figsize=(6,5))
plot = sns.boxplot(x="Question", y="Response", data=bgDfLikert, showcaps=True,showmeans=False,**boxStyle)
plt.xticks(rotation=45, ha="right")

for i, question in enumerate(likertQs):
    mean_val = bgDfLikert[bgDfLikert["Question"]==question]["Response"].mean()
    line , = plot.plot([i-0.4, i+0.4], [mean_val, mean_val], color='red', linestyle='dotted', linewidth=2)
    line.set_dashes([4.5, 2])
# Add grid lines
plot.grid(axis='y', linestyle='--', alpha=0.5)  # horizontal grid lines
plot.grid(axis='x', linestyle='--', alpha=0.2)  # horizontal grid lines

plt.tight_layout(pad=2)
plt.savefig("LikertQuestionResponses-Boxplot.pgf")


### yes/no plot 
questions = ["(8) Played Drums Before", "(10) Had Formal Lessons"]

yes_counts = [bgDf[q].value_counts().get(1, 0) for q in questions]
no_counts = [bgDf[q].value_counts().get(0, 0) for q in questions]

plt.figure(figsize=(4,2))
plt.bar(questions, yes_counts, color='salmon', edgecolor='white', label='Yes')
plt.bar(questions, no_counts, bottom=yes_counts, color='lightblue', edgecolor='white', label='No')

plt.ylabel("Number of Participants")
plt.legend(frameon=False)
plt.tight_layout()
plt.savefig("DrumLessonsBar.pgf")