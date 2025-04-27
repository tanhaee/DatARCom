using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// This fix is necessary since <see cref="Microsoft.MixedReality.Toolkit.Utilities.MeshOutline" /> does not respect dynamically assigned materials. 
/// The materials in the topic model spheres are dynamically assigned.
public class OutlineMaterialFix : MonoBehaviour
{
    private Material[] oldMaterials;
    private MeshOutline meshOutline;

    public void Start()
    {
        meshOutline = GetComponent<MeshOutline>();
    }

    private void StorePreviousMaterials()
    {
        oldMaterials = GetComponent<MeshRenderer>().materials;
    }

    private void RestorePreviousMaterials()
    {
        GetComponent<MeshRenderer>().materials = oldMaterials;
    }

    public void OverrideOldMaterials(Material[] newOldMaterials)
    {
        oldMaterials = newOldMaterials;
    }

    public void EnableMeshOutline(ManipulationEventData e)
    {
        EnableMeshOutline(e.Pointer);
    }

    public void EnableMeshOutline(FocusEventData e)
    {
        EnableMeshOutline(e.Pointer);
    }

    private void EnableMeshOutline(IMixedRealityPointer pointer)
    {
        if(pointer == null)
        {
            Debug.Log("pointer is null");
            return;
        }

        if (DatARService.Instance.currentFocusTarget == null)
        {
            StorePreviousMaterials();
            meshOutline.enabled = true;
            DatARService.Instance.currentFocusTarget = pointer.Result.CurrentPointerTarget;
        }
        else if (DatARService.Instance.currentFocusTarget != gameObject)
        {
            var omf = DatARService.Instance.currentFocusTarget.GetComponent<OutlineMaterialFix>();
            if(omf != null)
            {
                omf.DisableMeshOutline();
                StorePreviousMaterials();
                meshOutline.enabled = true;
                DatARService.Instance.currentFocusTarget = pointer.Result.CurrentPointerTarget;
            }
        }
    }

    public void DisableMeshOutline()
    {
        if (DatARService.Instance.currentFocusTarget == gameObject)
        {
            meshOutline.enabled = false;
            RestorePreviousMaterials();
            DatARService.Instance.currentFocusTarget = null;
        }
    }
}
