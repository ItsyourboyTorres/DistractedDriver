using System.Collections.Generic;
using UnityEngine;

public class PhoneMinigameSelector : MonoBehaviour
{
    [System.Serializable]
    public class MinigameEntry
    {
        public PhoneMinigameType gameType;
        public GameObject rootObject;

        [Header("Managers")]
        public FlyMinigameManager flyFlappyManager;

        [Header("Unlock")]
        public bool unlocked;
        public bool removed;
    }

    [Header("Minigames")]
    public List<MinigameEntry> minigames = new List<MinigameEntry>();

    public MinigameEntry CurrentEntry { get; private set; }

    public void OpenRandomUnlockedGame()
    {
        HideAllGames();

        List<MinigameEntry> unlockedGames = new List<MinigameEntry>();

        foreach (MinigameEntry game in minigames)
        {
            if (IsPlayable(game))
                unlockedGames.Add(game);
        }

        if (unlockedGames.Count == 0)
        {
            Debug.LogWarning("[MinigameSelector] No unlocked minigames found.");
            return;
        }

        CurrentEntry = unlockedGames[Random.Range(0, unlockedGames.Count)];
        CurrentEntry.rootObject.SetActive(true);

        switch (CurrentEntry.gameType)
        {
            case PhoneMinigameType.FlyFlappy:
                if (CurrentEntry.flyFlappyManager != null)
                    CurrentEntry.flyFlappyManager.StartGame();
                break;

            case PhoneMinigameType.PixelJump:
                Debug.Log("[MinigameSelector] PixelJump selected, but no manager is hooked up yet.");
                break;

            case PhoneMinigameType.Game3:
            case PhoneMinigameType.Game4:
            case PhoneMinigameType.Game5:
                Debug.Log("[MinigameSelector] Placeholder minigame selected: " + CurrentEntry.gameType);
                break;
        }

        Debug.Log("[MinigameSelector] Opened: " + CurrentEntry.gameType);
    }

    public void ResetCurrentGame()
    {
        if (CurrentEntry == null) return;

        switch (CurrentEntry.gameType)
        {
            case PhoneMinigameType.FlyFlappy:
                if (CurrentEntry.flyFlappyManager != null)
                    CurrentEntry.flyFlappyManager.ResetGame();
                break;

            case PhoneMinigameType.PixelJump:
                Debug.Log("[MinigameSelector] Reset PixelJump when its manager exists.");
                break;

            case PhoneMinigameType.Game3:
            case PhoneMinigameType.Game4:
            case PhoneMinigameType.Game5:
                break;
        }
    }

    public void HideAllGames()
    {
        foreach (MinigameEntry game in minigames)
        {
            if (game != null && game.rootObject != null)
                game.rootObject.SetActive(false);
        }

        CurrentEntry = null;
    }

    public void UnlockGame(PhoneMinigameType type)
    {
        foreach (MinigameEntry game in minigames)
        {
            if (game.gameType == type)
            {
                game.unlocked = true;
                Debug.Log("[MinigameSelector] Unlocked: " + type);
                return;
            }
        }
    }

    public bool IsUnlocked(PhoneMinigameType type)
    {
        foreach (MinigameEntry game in minigames)
        {
            if (game.gameType == type)
                return game.unlocked;
        }

        return false;
    }
    public bool IsPlayable(MinigameEntry entry)
    {
        return entry != null && entry.unlocked && !entry.removed && entry.rootObject != null &&
            entry.gameType == PhoneMinigameType.FlyFlappy && entry.flyFlappyManager != null;
    }
    public bool CanRemoveGame => minigames.FindAll(IsPlayable).Count > 1;
    public bool RemoveRandomPlayableGame()
    {
        var candidates = minigames.FindAll(IsPlayable);
        if (candidates.Count <= 1) return false;
        var chosen = candidates[Random.Range(0, candidates.Count)];
        if (CurrentEntry == chosen) { ResetCurrentGame(); HideAllGames(); }
        chosen.removed = true;
        return true;
    }
    public List<int> CaptureRemovedGames()
    {
        var result = new List<int>();
        for (int i = 0; i < minigames.Count; i++) if (minigames[i] != null && minigames[i].removed) result.Add(i);
        return result;
    }
    public void RestoreRemovedGames(List<int> indexes)
    {
        foreach (var entry in minigames) if (entry != null) entry.removed = false;
        foreach (int i in indexes)
            if (i >= 0 && i < minigames.Count && minigames[i] != null) minigames[i].removed = true;
    }

}
