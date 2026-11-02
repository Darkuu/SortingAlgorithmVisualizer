# Sorting Visualizer

An interactive Windows application built with C# and WPF that visualizes sorting algorithms using animated bars and pitch-based audio.

![alt text](https://github.com/Darkuu/Darkuu.github.io/blob/main/Thumbnails/SortingAlgorithmVisualizer1.png " Logo Title Text 1")

Each bar represents a number. Its height indicates its value, while highlight colors show the positions involved in comparisons and updates.

## Features

- Five sorting algorithms:
  - Bubble Sort
  - Selection Sort
  - Insertion Sort
  - Merge Sort
  - Quick Sort
    (More to be added soon, probably)
    
- Adjustable bar count from 2 to 1,000
- Adjustable  delay
- Shuffle, start, and stop controls
- Customizable bar and highlight colors
- Optional sound based on the highlighted bar's value
  
- Four selectable audio waveforms:
  - Sine
  - Triangle
  - Square
  - Sawtooth
  (More to be added soon, probably)


## Requirements

- Windows
- A .NET SDK compatible with the target framework in the project's `.csproj`
- JetBrains Rider, Visual Studio, or another suitable editor

The application uses [NAudio](https://github.com/naudio/NAudio) for audio output. I'm sure there might be better options, but i wanted to try this one.

## Getting Started

1. Clone or download this repository.
2. Open the solution or project in Rider or Visual Studio.
3. Restore NuGet packages. (NAudio)
4. Build and run the application.


## Usage

1. Enter a bar count and click **Apply**.
2. Select a sorting algorithm.
3. Choose your preferred colors. 
4. Enable **Sound** and select a waveform. 
5. Click **Start**.


Sound, colors, and delay can be changed while sorting.
Changing the bar count, shuffling, and algorithm selection are hoqwever disabled during playback.

## How It Works

The application separates sorting logic from visualization:

- `SortingAlgorithms.cs` generates sorting steps using `yield return`.
- Each `SortStep` contains an array snapshot and highlight indices.
- `MainWindow.xaml` defines the interface.
- `MainWindow.xaml.cs` plays the steps and updates reusable rectangles.
- `TonePlayer.cs` generates audio and maps bar values to frequencies.

Smaller values produce lower pitches; larger values produce higher pitches.
Pitch follows the highlighted value, not overall sorting progress.

#

## Notes

- This is an educational visualizer, not a performance benchmark.
- Array snapshots, drawing, and animation delays add overhead. I could have probably optimized it more, but this was a learning experience in using something other than Windows Forms.
- Audio is a continuous changing tone, rather than separate triggered sounds.
- Start with low speaker or headphone volume.
