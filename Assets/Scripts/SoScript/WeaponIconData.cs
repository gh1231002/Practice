using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponIconData", menuName = "Scriptable Objects/WeaponIconData")]
public class WeaponIconData : ScriptableObject
{
    [Serializable]
    public class WeaponLayerData
    {
        public string LayerName;
        public Sprite Icon;
    }
    [SerializeField] List<WeaponLayerData> WeaponLayerList;

    public Sprite GetIconByLayerName(string layerName)
    {
        var data = WeaponLayerList.Find(x => x.LayerName == layerName);
        return data.Icon;
    }
}
