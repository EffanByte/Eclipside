using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AsepriteFrame
{
    public string filename;
    public Rect frame;
}

[System.Serializable]
public class AsepriteRoot
{
    public List<AsepriteFrame> frames;
}

public class AsepriteLoader : MonoBehaviour
{
    public TextAsset jsonFile;
    public Texture2D spriteSheet;

    void Start()
    {
        var data = JsonUtility.FromJson<AsepriteRoot>(jsonFile.text);
        
        foreach (var frame in data.frames)
        {
            var sprite = Sprite.Create(
                spriteSheet,
                new Rect(frame.frame.x, frame.frame.y, frame.frame.width, frame.frame.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            var go = new GameObject(frame.filename, typeof(SpriteRenderer));
            go.GetComponent<SpriteRenderer>().sprite = sprite;
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(frame.frame.x, frame.frame.y, 0);
        }
    }
}
