using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("プレイヤーのパラメーター")]

    public float currentHP = 100;
    public float maxHP = 100;

    [Header("プレイヤーのパラメーター")]
    public GameObject smooth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void DialogPlayerStart() //会話が始まった時
    {
        smooth?.SetActive(false);
    }

    public void DialogPlayerEnd()  //会話が終了した時
    {
        smooth?.SetActive(true);
    }
}
