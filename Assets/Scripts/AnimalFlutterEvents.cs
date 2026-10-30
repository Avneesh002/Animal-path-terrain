using System;
using UnityEngine;
using UnityKit;

/// <summary>Sends animal interaction events to the unity_kit Flutter bridge.</summary>
public static class AnimalFlutterEvents
{
    [Serializable]
    private sealed class Payload
    {
        public string type;
        public AnimalData data;
    }

    [Serializable]
    private sealed class AnimalData
    {
        public string animalId;
    }

    public static void Send(string type, string animalId)
    {
        if (string.IsNullOrWhiteSpace(animalId))
        {
            Debug.LogWarning("Animal interaction has no animalId; Flutter event was skipped.");
            return;
        }

        var payload = new Payload
        {
            type = type,
            data = new AnimalData { animalId = animalId }
        };

        try
        {
            NativeAPI.SendToFlutter(JsonUtility.ToJson(payload));
        }
        catch (Exception error)
        {
            Debug.LogWarning($"Could not send animal event to Flutter: {error.Message}");
        }
    }
}
