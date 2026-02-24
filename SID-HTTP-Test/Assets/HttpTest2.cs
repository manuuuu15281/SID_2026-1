using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HttpTest2: MonoBehaviour
{
    [SerializeField]
    private int characterID = 12;

    [SerializeField]
    private string URL = "https://rickandmortyapi.com/api/character";

    [SerializeField]
    private RawImage CharacterImage;

     void Start() {
        StartCoroutine(GetText());
    }
 
    IEnumerator GetText() {
        UnityWebRequest www = UnityWebRequest.Get(URL + "/" + characterID);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
            if(www.responseCode == 404)
            {
                Debug.Log("Character not found");
            }
        }
        else 
        {
            Character character = JsonUtility.FromJson<Character>(www.downloadHandler.text);
            Debug.Log(character.name + " is a " + character.species );

            StartCoroutine(GetTexture(character.image));

        }
    }

    IEnumerator GetTexture(string imageUrl)
    {
    using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imageUrl))
        {
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
            {
            Debug.LogError("Texture error: " + uwr.error);
            }
        else
            {
            var texture = DownloadHandlerTexture.GetContent(uwr);
            CharacterImage.texture = texture;
            }
        }
    }
}

class Character
{
    public int id;
    public string name;
    public string species;
    public string image;

}
