# AR Gamified Drums

An AR project built in Unity designed to aid in teaching the drums, incorporating motion captured action observation methods and gamification elements similar to those found in a rhythm game. Designed for use with a Meta Quest 3 headset.

The Unity project is available in the `Unity Project` directory, and Python scripts along with raw data used for analysis is available in the `Data Analysis` directory.

To open the project, clone the repository or the project directory and import it into Unity. It was developed in version 6000.1.6f1. 

A build of the latest version is available in the Releases tab. This can be installed onto a Meta Quest 3 via the Meta Quest Developer Hub. Alternatively, Unity can be set up to build and install the application directly to the headset for use in testing. More information on setting up the Quest 3 headset in this way is available [in the Meta Horizon documentation](https://developers.meta.com/horizon/documentation/unity/unity-tutorial-hello-vr/). 

To take proper advantage of the application's capabilities, a MIDI drum kit is required and should be connected via a MIDI-USB interface connected to the headset. By default, the rhythm taught in our preliminary user study is included and pre-made recordings can be played within the application. Further MIDI files must be included in the `Unity Project/Assets/StreamingAssets/MIDI Files.zip` file which will then make them available in the application (after a fresh build). 

Overview of key game's scripts, found in `Unity Project/Assets/Scripts`:
- **MIDIReader.cs** Reads MIDI files, returning a list of notes and a file's tempo map.
- **RecordingMenu.cs & UserInputDialogue.cs** Handles instantiation of the Recording Menu and its inputs.
- **DemonstrationPlayer.cs** Plays 'components' (individual MIDI files) of a rhythm from a list, with controllable playback modes. Used to evaluate user performance in the study.
- **ManualAlign.cs** Handles alignment of virtual drums and other elements of the scene with the physical environment, manipulated via controller motion.
- **FileManager.cs** Handles extraction of MIDI and recording files, kept in ZIP files to allow bundling with the .apk, when the application is loaded.
- **NoteSpawner.cs** Handles the spawning of note indicators for our novel 'Falling Notes' method. Current MIDI time is calculated in this script, and bass drum leg animations are also trigged when the correct note is spawned.
- **PlaybackManager.cs** Global state machine that manages the playback and recording of demonstrations, including serialisation of note data.
- **DrumHit.cs** Attached to each virtual drum, handles user drum hits, checking whether they occured within the hit window. Also triggers accuracy feedback. The colour of the drum can be controlled here.
- **ControllerRecorder.cs** Handles recording of hand and controller tracking for AO demonstrations. Also contains some of the classes used for serialisation.


![com oculus vrshell-20250902-205112 (Small)](https://github.com/user-attachments/assets/d3d31eeb-1f55-48d5-9895-10c99a794015)
