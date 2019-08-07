﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Name and idea totally not stolen directly from Beat Saber
public class BeatmapObjectCallbackController : MonoBehaviour {

    [SerializeField] NotesContainer notesContainer;
    [SerializeField] EventsContainer eventsContainer;
    [SerializeField] BPMChangesContainer bpmContainer;

    [SerializeField] AudioTimeSyncController timeSyncController;

    [Tooltip("The amount of time (in Note Time, or beats) to offset the detection by")]
    [SerializeField] public float offset;

    [SerializeField] int nextNoteIndex = 0;
    [SerializeField] int nextEventIndex = 0;

    public int NextNoteIndex {
        get { return nextNoteIndex; }
    }

    public int NextEventIndex
    {
        get { return nextEventIndex; }
    }

    float curNoteTime;

    public Action<bool, int, BeatmapObject> NotePassedThreshold;
    public Action<bool, int, BeatmapObject> EventPassedThreshold;
    public Action<bool, int> RecursiveNoteCheckFinished;
    public Action<bool, int> RecursiveEventCheckFinished;
    
    private List<BeatmapObjectContainer> nextEvents = new List<BeatmapObjectContainer>();

    private void OnEnable() {
        timeSyncController.OnPlayToggle += OnPlayToggle;
        timeSyncController.OnTimeChanged += OnTimeChanged;
    }

    private void OnDisable() {
        timeSyncController.OnPlayToggle -= OnPlayToggle;
        timeSyncController.OnTimeChanged -= OnTimeChanged;
    }

    private void OnPlayToggle(bool playing) {
        CheckAllNotes(false);
        CheckAllEvents(false);
    }

    private void OnTimeChanged() {
        //CheckAllNotes(false);
        //CheckAllEvents(false);
    }

    private void LateUpdate() {
        if (timeSyncController.IsPlaying) {
            curNoteTime = timeSyncController.CurrentBeat;
            RecursiveCheckNotes(true, true);
            RecursiveCheckEvents(true, true);
        }
    }

    private void CheckAllNotes(bool natural)
    {
        curNoteTime = timeSyncController.CurrentBeat;
        nextNoteIndex = 0;
        RecursiveCheckNotes(true, natural);
        if (RecursiveNoteCheckFinished != null) RecursiveNoteCheckFinished(natural, nextNoteIndex - 1);
    }

    private void CheckAllEvents(bool natural)
    {
        nextEvents = new List<BeatmapObjectContainer>(eventsContainer.loadedEvents);
        nextEventIndex = 0;
        RecursiveCheckEvents(true, natural);
        if (RecursiveEventCheckFinished != null) RecursiveEventCheckFinished(natural, nextEventIndex - 1);
    }

    private void RecursiveCheckNotes(bool initial, bool natural) {
        if (nextNoteIndex >= notesContainer.loadedNotes.Count) return;
        if ((curNoteTime + offset) > notesContainer.loadedNotes[nextNoteIndex].objectData._time) {
            if (natural && NotePassedThreshold != null) NotePassedThreshold.Invoke(initial, nextNoteIndex, notesContainer.loadedNotes[nextNoteIndex].objectData);
            nextNoteIndex++;
            RecursiveCheckNotes(false, natural);
        }
    }

    private void RecursiveCheckEvents(bool init, bool natural)
    {
        if (nextEvents.Count == 0) return;
        IEnumerable<BeatmapObjectContainer> passed = new List<BeatmapObjectContainer>(nextEvents.Where(x => x.objectData._time < curNoteTime + offset));
        foreach (BeatmapObjectContainer newlyAdded in passed)
        {
            if (natural && EventPassedThreshold != null) EventPassedThreshold.Invoke(false, nextEventIndex, newlyAdded.objectData);
            nextEvents.Remove(newlyAdded);
            nextEventIndex++;
        }
        /*if (nextEventIndex >= eventsContainer.loadedEvents.Count) return;
        if ((curNoteTime + offset) > eventsContainer.loadedEvents[nextEventIndex].objectData._time)
        {
            if (natural && EventPassedThreshold != null) EventPassedThreshold.Invoke(init, nextEventIndex, eventsContainer.loadedEvents[nextEventIndex].objectData);
            nextEventIndex++;
            RecursiveCheckEvents(false, natural);
        }*/
    }

}