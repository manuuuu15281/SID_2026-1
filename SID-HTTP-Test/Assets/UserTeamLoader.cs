using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UserTeamLoader : MonoBehaviour
{
    [Header("Endpoints")]
    [SerializeField] private string usersURL =
        "https://my-json-server.typicode.com/manuuuu15281/SID_2026-1/users";

    [SerializeField] private string characterURL =
        "https://rickandmortyapi.com/api/character";

    [Header("UI")]
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private TMP_InputField userIdInput;

    // Tu proyecto ya usa arrays (en tu Inspector están en size 1)
    [SerializeField] private RawImage[] characterImages;     // size 1
    [SerializeField] private TMP_Text[] characterNames;      // size 1
    [SerializeField] private TMP_Text[] characterSpecies;    // size 1
    [SerializeField] private TMP_Text[] characterStatus;     // size 1

    // --- Deck state ---
    private int[] currentDeck;
    private int currentCardIndex = 0;
    private string currentUsername = "";

    private void Start()
    {
        LoadUserByID(1);
    }

    // Conectar esto al botón Find
    public void OnFindClicked()
    {
        if (userIdInput == null)
        {
            Debug.LogWarning("UserIdInput no asignado en el Inspector.");
            return;
        }

        if (int.TryParse(userIdInput.text, out int id))
            LoadUserByID(id);
        else
            Debug.LogWarning("El ID debe ser un número entero.");
    }

    // Conectar esto a botón "Siguiente carta"
    public void OnNextCard()
    {
        if (currentDeck == null || currentDeck.Length == 0) return;

        currentCardIndex = (currentCardIndex + 1) % currentDeck.Length;

        StopAllCoroutines();
        StartCoroutine(GetCharacter(currentDeck[currentCardIndex], 0));
        UpdateHeader();
    }

    // Conectar esto a botón "Anterior carta"
    public void OnPrevCard()
    {
        if (currentDeck == null || currentDeck.Length == 0) return;

        currentCardIndex = (currentCardIndex - 1 + currentDeck.Length) % currentDeck.Length;

        StopAllCoroutines();
        StartCoroutine(GetCharacter(currentDeck[currentCardIndex], 0));
        UpdateHeader();
    }

    public void LoadUserByID(int userID)
    {
        StopAllCoroutines();
        StartCoroutine(GetUser(userID));
    }

    private IEnumerator GetUser(int id)
    {
        using UnityWebRequest www = UnityWebRequest.Get(usersURL + "/" + id);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"GetUser error: {www.responseCode} - {www.error}");
            yield break;
        }

        User user = JsonUtility.FromJson<User>(www.downloadHandler.text);
        if (user == null)
        {
            Debug.LogError("No se pudo parsear el User.");
            yield break;
        }

        currentUsername = user.username;
        currentDeck = user.characters;
        currentCardIndex = 0;
        UpdateHeader();
        
        

        // Limpia UI (slot 0)
        ClearSlot0();

        
        

        if (currentDeck != null && currentDeck.Length > 0)
        {
            yield return GetCharacter(currentDeck[currentCardIndex], 0);
        }
        else
        {
            // Si el jugador no tiene cartas
            if (characterNames.Length > 0) characterNames[0].text = "Sin cartas";
            if (characterSpecies.Length > 0) characterSpecies[0].text = "";
            if (characterStatus.Length > 0) characterStatus[0].text = "";
        }
    }

    private void UpdateHeader()
    {
        if (userNameText == null) return;

        int total = (currentDeck == null) ? 0 : currentDeck.Length;

        if (string.IsNullOrEmpty(currentUsername))
            currentUsername = userNameText.text; // fallback, por si acaso

        userNameText.text = (total == 0)
            ? currentUsername
            : $"{currentUsername} (Carta {currentCardIndex + 1}/{total})";
    }

    private void ClearSlot0()
    {
        if (characterImages != null && characterImages.Length > 0 && characterImages[0] != null)
            characterImages[0].texture = null;

        if (characterNames != null && characterNames.Length > 0 && characterNames[0] != null)
            characterNames[0].text = "";

        if (characterSpecies != null && characterSpecies.Length > 0 && characterSpecies[0] != null)
            characterSpecies[0].text = "";

        if (characterStatus != null && characterStatus.Length > 0 && characterStatus[0] != null)
            characterStatus[0].text = "";
    }

    private IEnumerator GetCharacter(int characterID, int slot)
    {
        using UnityWebRequest www = UnityWebRequest.Get(characterURL + "/" + characterID);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"GetCharacter error: {www.responseCode} - {www.error}");
            yield break;
        }

        Apiclases character = JsonUtility.FromJson<Apiclases>(www.downloadHandler.text);
        if (character == null)
        {
            Debug.LogError("No se pudo parsear el character.");
            yield break;
        }

        if (slot < characterNames.Length && characterNames[slot] != null)
            characterNames[slot].text = character.name;

        if (slot < characterSpecies.Length && characterSpecies[slot] != null)
            characterSpecies[slot].text = "Especie: " + character.species;

        if (slot < characterStatus.Length && characterStatus[slot] != null)
            characterStatus[slot].text = "Estado: " + character.status;

        if (!string.IsNullOrEmpty(character.image))
            yield return GetTexture(character.image, slot);
    }

    private IEnumerator GetTexture(string url, int slot)
    {
        using UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url);
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success) yield break;

        if (slot < characterImages.Length && characterImages[slot] != null)
            characterImages[slot].texture = DownloadHandlerTexture.GetContent(uwr);
    }
}

[Serializable]
public class User
{
    public int id;
    public string username;
    public int[] characters;
}

[Serializable]
public class Apiclases
{
    public int id;
    public string name;
    public string status;
    public string species;
    public string image;
}