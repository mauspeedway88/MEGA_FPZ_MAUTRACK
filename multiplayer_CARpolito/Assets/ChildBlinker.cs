using UnityEngine;
using System.Collections;

public class ChildBlinker : MonoBehaviour
{
    public float visibleTime = 1f;
    public float hiddenTime = 2f;

    private GameObject[] _children;

    private void Awake()
    {
        _children = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            _children[i] = transform.GetChild(i).gameObject;
    }

    private void OnEnable()
    {
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            SetChildrenActive(true);
            yield return new WaitForSeconds(visibleTime);

            SetChildrenActive(false);
            yield return new WaitForSeconds(hiddenTime);
        }
    }

    private void SetChildrenActive(bool state)
    {
        for (int i = 0; i < _children.Length; i++)
            if (_children[i] != null)
                _children[i].SetActive(state);
    }
}
