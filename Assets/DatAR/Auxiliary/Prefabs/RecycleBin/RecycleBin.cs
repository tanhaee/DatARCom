using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class RecycleBin : MonoBehaviour
{
    void OnTriggerEnter(Collider collision)
    {
        if (collision.tag.Equals("Deletable"))
        {
            Destroy(collision.gameObject);
        }
    }
}
