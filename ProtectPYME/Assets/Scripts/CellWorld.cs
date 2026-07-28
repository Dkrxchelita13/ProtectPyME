using UnityEngine;

public class CellWorld : MonoBehaviour
{
    public TextMesh text;
    private SpriteRenderer sr;

    private int x;
    private int y;
    private CrosswordController controller;
    private int dirX;
    private int dirY;
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }
    private bool locked = false;

    public void Init(int x, int y, CrosswordController controller)
    {
        this.x = x;
        this.y = y;
        this.controller = controller;

        text.text = "";
        sr.color = Color.white;
    }
    public void SetDirection(int dx, int dy)
    {
        dirX = dx;
        dirY = dy;
    }


    public bool IsLocked()
    {
        return locked;
    }
    public void SetLetter(string letter)
    {

        if (locked) return;

        text.text = letter;

        Debug.Log("LETRA PUESTA: " + letter);

        controller.OnLetterChanged(x, y, letter);
    }
    public void ClearLetter()
    {
        text.text = "";

        controller.OnLetterChanged(x, y, "");
    }
    public bool IsPosition(int px, int py)
    {
        return x == px && y == py;
    }

    public void SetCorrect()
    {
        text.color = Color.green;
    }
    public void SetWrong()
    {
        text.color = Color.red;
    }

    public void SetNormal()
    {
        text.color = Color.white;
    }
    public void Select()
    {
        sr.color = new Color(1f, 0.9f, 0.2f);
    }

    public void Deselect()
    {
        sr.color = new Color(1f, 1f, 1f, 0.9f);
    }
    public void LockCell()
    {
        locked = true;
        sr.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    }
    public void UnlockCell()
    {
        locked = false;
        sr.color = new Color(1f, 1f, 1f, 0.9f); 
    }
    public void DisplayLetter(string letter)
    {
        text.text = letter;
    }
    public int GetX()
    {
        return x;
    }

    public int GetY()
    {
        return y;
    }
    public int GetDirX()
    {
        return dirX;
    }

    public int GetDirY()
    {
        return dirY;
    }
}