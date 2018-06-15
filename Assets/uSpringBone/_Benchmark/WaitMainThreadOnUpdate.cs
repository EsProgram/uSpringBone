using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;


/// <summary>
/// ベンチマーク用
/// Update呼び出しタイミングである程度の負荷の処理を行うことを想定し、ThreadをSleepさせる
/// </summary>
public class WaitMainThreadOnUpdate : MonoBehaviour
{
    public int waitMilliseconds = 1;

    void Update()
    {
        Thread.Sleep(waitMilliseconds);
    }
}