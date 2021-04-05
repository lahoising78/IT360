﻿using System.Collections.Generic;
using UnityEngine;

public class Song : MonoBehaviour
{
    [SerializeField] private Transform barsTransform = null;
    [SerializeField] private GameObject barPrefab = null;
    [SerializeField] private GameObject extraLinePrefab = null;
    [SerializeField] private GameObject[] notePrefabs = null;
    
    [SerializeField] private AudioSource metronomeTic = null;
    private float secsPerBeat = 0.0f;
    private float nextBeatTime = 0.0f;
    
    private Queue<ProcessedNote> notes = new Queue<ProcessedNote>();
    private RawSong rawSong = null;
    private Transform currentBar = null;
    
    private float spaceBetweenNotesY = 0.0f;
    private float spaceForNotesInBar = 0.0f;
    private float barWidth = 0.0f;
    private float c4PosY = 0.0f;
    private float barNoteStartOffset = 0.0f;

    private float staffSpeed = 0.0f;
    private int ticCount = 0;

    void Awake()
    {
        Transform barDimensions = GameObject.Find("BarDimensions").transform;
        if(barDimensions)
        {
            Transform min = barDimensions.GetChild(0);
            Transform max = barDimensions.GetChild(1);
            barWidth =  max.position.x -
                        min.position.x;

            spaceBetweenNotesY =    max.position.y -
                                    min.position.y;
            spaceBetweenNotesY /= 8.0f;

            c4PosY = min.position.y - spaceBetweenNotesY * 2.0f;

            Transform notesSpaceMin = barDimensions.GetChild(2);
            Transform notesSpaceMax = barDimensions.GetChild(3);
            spaceForNotesInBar =  notesSpaceMax.position.x -
                                    notesSpaceMin.position.x;

            barNoteStartOffset = barDimensions.position.x - notesSpaceMin.position.x;
        }

        Destroy(barsTransform.GetChild(0).gameObject);

        if(!metronomeTic) metronomeTic = GetComponent<AudioSource>();
        SetupSong(new RawSong(), 60);
    }

    void Update()
    {
        float currentTime = Time.time;
        if(currentTime - nextBeatTime >= 0.0f)
        {
            nextBeatTime = currentTime + secsPerBeat;
            metronomeTic.Play();
            ticCount++;
        }
        
        if(ticCount > 4)
        {
            Vector3 newPosition = barsTransform.position;
            newPosition.x -= staffSpeed * Time.deltaTime;
            barsTransform.position = newPosition;
        }
    }

    public void SetupSong(RawSong song, int bpm)
    {
        this.rawSong = song;
        this.secsPerBeat = 60.0f / bpm;

        notes.Clear();
        float time = 0.0f;
        int barCount = 0;
        float currentBarCompletion = 1.0f;
        float currentOffset = 0.0f;
        foreach(Note note in song.GetNotes())
        {
            AddNote(note, ref time, bpm);
            PlaceNote(note, ref barCount, ref currentBarCompletion, ref currentOffset);
        }
    }

    private void AddNote(Note note, ref float time, int bpm)
    {
        float secsPerWholeNote = bpm / 60.0f * 4.0f;
        staffSpeed = barWidth / secsPerWholeNote;
        ProcessedNote processed = new ProcessedNote();
        processed.note = note;
        
        float noteDuration = (float)(1 << (int)(note.rhythm));
        noteDuration = secsPerWholeNote / noteDuration;
        processed.time = time = time + noteDuration;

        notes.Enqueue(processed);
    }

    private void PlaceNote(Note note, ref int barCount, ref float currentBarCompletion, ref float currentOffset)
    {
        if(currentBarCompletion == 1.0f)
        {
            InstantiateBar(ref barCount, ref currentOffset);
            currentBarCompletion = 0.0f;
        }

        int keyDiff = (int)note.key - (int)Player.Key.C;
        
        // put silence in the middle of the pentagram
        if(note.key == Player.Key.SILENCE) keyDiff += (int)Player.Key.C - (int)Player.Key.B; 

        const int centralCOctave = 4;
        int octaveDiff = note.octave - centralCOctave;

        Vector2 offset = new Vector2(
            spaceForNotesInBar * currentBarCompletion,
            spaceBetweenNotesY * (keyDiff + octaveDiff * 8)
        );

        Vector3 notePosition = new Vector3(
            currentBar.position.x - barNoteStartOffset + offset.x,
            c4PosY + offset.y,
            currentBar.position.z - 0.001f
        );

        int prefabIndex = (int)note.rhythm;
        if(prefabIndex >= notePrefabs.Length) prefabIndex = notePrefabs.Length - 1;
        GameObject notePrefab = notePrefabs[ prefabIndex ];
        InstantiateBarElement(notePrefab, notePosition);
        if(offset.y <= 0.0f)
            InstantiateBarElement(extraLinePrefab, notePosition);
        currentBarCompletion += 1.0f / (float)(1 << (int)note.rhythm);
    }

    private void InstantiateBar(ref int barCount, ref float currentOffset)
    {
        Vector3 newBarPosition = new Vector3(
            barsTransform.position.x + barCount * barWidth,
            barsTransform.position.y,
            barsTransform.position.z
        );

        currentBar = Instantiate(
            barPrefab,
            newBarPosition,
            Quaternion.identity,
            barsTransform
        ).transform;

        barCount++;
        currentOffset += barWidth;
    }

    private GameObject InstantiateBarElement(GameObject prefab, Vector3 position)
    {
        return Instantiate(
            prefab,
            position,
            Quaternion.identity,
            currentBar
        );
    }
    
    private struct ProcessedNote
    {
        public Note note;
        public float time;
    }
}
