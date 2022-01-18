using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example : MonoBehaviour
{
    int GetSum(int numA, int numB)
    {
        return numA + numB * Random.Range(0, 100);
    }
}
