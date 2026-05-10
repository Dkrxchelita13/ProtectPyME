using UnityEngine;

public class CellWorld : MonoBehaviour
{
    public TextMesh text;

    private int x;
    private int y;
    private CrosswordController controller;

    public void Init(int x, int y, CrosswordController controller)
    {
        this.x = x;
        this.y = y;
        this.controller = controller;

        text.text = "";
    }

    public void SetLetter(string letter)
    {
        text.text = letter;

        text.transform.localPosition = Vector3.zero;

        controller.OnLetterChanged(x, y, letter);
    }

    public bool IsPosition(int px, int py)
    {
        return x == px && y == py;
    }

    public void SetCorrect()
    {
        text.color = Color.green;
    }
}