﻿namespace FSharp.Windows.Sample

open System
open System.Diagnostics
open System.Reactive.Linq

type StopWatchObservable(frequency, failureFrequencyInSeconds) =
    let watch = Stopwatch.StartNew()
    let paused = ref false
    let generateFailures = ref false

    member this.Pause() = 
        watch.Stop()
        paused := true
    member this.Start() = 
        watch.Start()
        paused := false
    member this.Restart() = 
        watch.Restart()
        paused := false

    member this.GenerateFailures with set value = generateFailures := value

    interface IObservable<TimeSpan> with
        member this.Subscribe observer = 
            Observable.Interval(period = frequency)
                .Where(fun _ -> not !paused)
                .Select(fun _ -> 
                    if !generateFailures && watch.Elapsed.TotalSeconds % failureFrequencyInSeconds < 1.0
                    then failwithf "failing every %.1f secs" failureFrequencyInSeconds
                    else watch.Elapsed)
                .Subscribe(observer)

