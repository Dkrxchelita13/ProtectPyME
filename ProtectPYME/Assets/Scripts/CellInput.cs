using UnityEngine;
using TMPro;

public class CellInput : MonoBehaviour
{
    public TMP_InputField input;
    public int x;
    public int y;

    private CrosswordController controller;

    public void Init(int posX, int posY, CrosswordController ctrl)
    {
        x = posX;
        y = posY;
        controller = ctrl;

        input.onValueChanged.AddListener(OnValueChanged);
    }

    void OnValueChanged(string value)
    {
        if (value.Length > 1)
            input.text = value.Substring(0, 1);

        controller.OnLetterChanged(x, y, input.text.ToUpper());
    }

    public void SetCorrect()
    {
        input.image.color = Color.green;
        input.interactable = false;
    }
}