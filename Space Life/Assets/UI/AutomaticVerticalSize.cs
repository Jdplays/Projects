using UnityEngine;

[ExecuteInEditMode]
public class AutomaticVerticalSize : MonoBehaviour
{
    public float childHeight = 35f;

    // Use this for initialization
    public void Start()
    {
        AdjustSize();
    }

    public void Update()
    {
        AdjustSize();
    }

    public void AdjustSize()
    {
        Vector2 size = this.GetComponent<RectTransform>().sizeDelta;
        size.y = this.transform.childCount * childHeight;
        this.GetComponent<RectTransform>().sizeDelta = size;
    }
}
