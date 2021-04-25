using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

///<summary>
/// A simple in-game GUI for Unity that allows tweaking of script fields 
/// and properties marked with [TweakableMember]
///</summary>
public class UnityTweakGUI : MonoBehaviour
{

    public Transform[] targetObjects;
    public int winWidth = 350;
    public int winHeight = 500;
    public bool expanded = false;

    private Dictionary<string, List<TweakableParam>> groupParamsMap;
    private string[] sortedGroups;

    private Vector2 scrollPosition;
    private Rect windowRect;

    private int headerHeight = 20;
    private int expandToggleWidth = 60;
    private GUIStyle groupTextStyle;

    void Start()
    {
        groupTextStyle = new GUIStyle();
        groupTextStyle.normal.textColor = Color.white;
        groupTextStyle.fontStyle = FontStyle.Bold;
        groupTextStyle.fontSize = 18;

        windowRect = new Rect(0, 0, winWidth, winHeight);
        scrollPosition = new Vector2();

        InitTweakableParams();
    }

    /// Init or re-init the tweakable params collection
    private void InitTweakableParams()
    {
        if (groupParamsMap == null)
        {
            groupParamsMap = new Dictionary<string, List<TweakableParam>>();
        }
        else
        {
            groupParamsMap.Clear();
        }

        // Search all objects in scene by default if none are specified
        if (targetObjects == null || targetObjects.Length == 0)
        {
            Debug.Log("No target transform set for TweakGUI, using all in scene.");
            targetObjects = FindAllRootTransformsInScene().ToArray();
        }

        // Traverse target transforms
        foreach (Transform t in targetObjects)
        {
            AddTweakableParamsForTransform(t);
        }

        // Sort groups alphabetically
        sortedGroups = new string[groupParamsMap.Count];
        groupParamsMap.Keys.CopyTo(sortedGroups, 0);
        Array.Sort<string>(sortedGroups);
    }

    /// Walks through the transform heirarchy and finds all [Tweakable] members of MonoBehaviours.
    private void AddTweakableParamsForTransform(Transform targetObj)
    {
        if (targetObj != null)
        {
            foreach (MonoBehaviour monoBehaviour in targetObj.GetComponents<MonoBehaviour>())
            {
                foreach (MemberInfo memberInfo in monoBehaviour.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (memberInfo is FieldInfo || memberInfo is PropertyInfo)
                    {
                        foreach (object attributeObj in memberInfo.GetCustomAttributes(true))
                        {
                            if (attributeObj is TweakableMemberAttribute)
                            {
                                var attribute = (TweakableMemberAttribute)attributeObj;
                                var tweakableParam = new TweakableParam(attribute, memberInfo, monoBehaviour);
                                List<TweakableParam> paramList;
                                if (groupParamsMap.TryGetValue(attribute.group, out paramList))
                                {
                                    paramList.Add(tweakableParam);
                                }
                                else
                                {
                                    paramList = new List<TweakableParam>();
                                    paramList.Add(tweakableParam);
                                    groupParamsMap.Add(attribute.group, paramList);
                                }
                            }
                        }
                    }
                }
            }

            // Add tweakable fields in child components recursively
            foreach (Transform child in targetObj)
            {
                AddTweakableParamsForTransform(child);
            }
        }
    }

    /// Draw the tweak GUI.
    void OnGUI()
    {

        // Scale GUI to make it useable on high-res displays (retina etc)
        var scale = 1f;
        if (Screen.dpi > 200)
        {
            scale = 3f;
        }
        else if (Screen.dpi > 100)
        {
            scale = 2f;
        }
        var scaleVec = new Vector2(scale, scale);
        GUIUtility.ScaleAroundPivot(scaleVec, Vector2.zero);

        if (expanded)
        {
            windowRect.width = winWidth;
            windowRect.height = winHeight;
        }
        else
        {
            windowRect.width = expandToggleWidth + 20;
            windowRect.height = headerHeight + 10;
        }

        // Draw window
        windowRect = GUILayout.Window(0, windowRect, delegate (int id)
        {
            GUI.DragWindow(new Rect(expandToggleWidth, 0, winWidth - expandToggleWidth, headerHeight));

            GUILayout.BeginVertical();
            expanded = GUI.Toggle(new Rect(0, 0, expandToggleWidth, headerHeight), expanded, "Expand");

            if (expanded)
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                GUILayout.BeginVertical();


                // Iterate over sorted param groups and lay out the right kind of GUI
                // controls for each tweakable parameter.
                foreach (var group in sortedGroups)
                {

                    GUILayout.Label(group, groupTextStyle);

                    List<TweakableParam> tweakableParams;
                    if (groupParamsMap.TryGetValue(group, out tweakableParams))
                    {

                        for (int i = 0; i < tweakableParams.Count; i++)
                        {
                            TweakableParam tweakableParam = tweakableParams[i];
                            TweakableMemberAttribute attr = tweakableParam.attribute;
                            MemberInfo memberInfo = tweakableParam.memberInfo;

                            object value = tweakableParam.GetMemberValue();
                            Type type = value.GetType();
                            object newValue = null;
                            string paramName = attr.displayName != ""
                                               ? attr.displayName
                                               : MemberInfoNameToParamName(memberInfo);

                            // Output the right GUI control

                            bool showHeaderLabel = true;
                            bool showValueLabel = true;

                            if (type == typeof(bool))
                            {
                                showHeaderLabel = false;
                            }

                            if (showHeaderLabel)
                            {
                                GUILayout.Label(paramName);
                            }
                            GUILayout.BeginHorizontal();

                            // Check the type of the member and draw the right control
                            if (type == typeof(float))
                            {
                                newValue = GUILayout.HorizontalSlider((float)value, attr.minValue, attr.maxValue);
                            }
                            else if (type == typeof(int))
                            {
                                newValue = (int)GUILayout.HorizontalSlider((int)value, attr.minValue, attr.maxValue);
                            }
                            else if (type == typeof(bool))
                            {
                                newValue = GUILayout.Toggle((bool)value, paramName);
                                showValueLabel = false;
                            }

                            if (showValueLabel)
                            {
                                GUILayout.Label(type == typeof(float) ? ((float)value).ToString("F3") : value.ToString(), GUILayout.Width(40));
                            }

                            GUILayout.EndHorizontal();

                            if (newValue != null)
                            {
                                tweakableParam.SetMemberValue(newValue);
                            }
                        }
                    }

                }

                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }, "Tweaks");
    }

    private static string MemberInfoNameToParamName(MemberInfo memberInfo)
    {
        var result = Regex.Replace(
            memberInfo.Name,
            @"((?<=[A-Z])([A-Z])(?=[a-z]))|((?<=[a-z]+)([A-Z]))", @" $0",
            RegexOptions.Compiled);
        result = result.Trim();
        result = result.Replace("_", "");
        return result.Substring(0, 1).ToUpper() + result.Substring(1);
    }

    private List<Transform> FindAllRootTransformsInScene()
    {
        List<Transform> roots = new List<Transform>();
        foreach (GameObject obj in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
        {
            if (obj.transform.parent == null)
            {
                roots.Add(obj.transform);
            }
        }
        return roots;
    }


    /// Private class that wraps a tweakable member
    private class TweakableParam
    {
        public TweakableMemberAttribute attribute;
        public MemberInfo memberInfo;
        private object ownerObject;
        private bool isField;

        public TweakableParam(TweakableMemberAttribute attr, MemberInfo memberInfo, object ownerObject)
        {
            this.attribute = attr;
            this.memberInfo = memberInfo;
            this.ownerObject = ownerObject;
            this.isField = memberInfo is FieldInfo;
            if (!(memberInfo is FieldInfo || memberInfo is PropertyInfo))
            {
                throw new ArgumentException("Member " + memberInfo.ToString() + " not supported.");
            }
        }

        public object GetMemberValue()
        {
            if (isField)
            {
                return ((FieldInfo)memberInfo).GetValue(ownerObject);
            }
            else
            {
                return ((PropertyInfo)memberInfo).GetValue(ownerObject, null);
            }
        }

        public void SetMemberValue(object value)
        {
            if (isField)
            {
                ((FieldInfo)memberInfo).SetValue(ownerObject, value);
            }
            else
            {
                ((PropertyInfo)memberInfo).SetValue(ownerObject, value, null);
            }
        }
    }
}


/// Attribute that can be used to mark fields or properties on MonoBehaivours as Tweakable,
/// letting them be tweaked in an in-game GUI.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
class TweakableMemberAttribute : Attribute
{
    public float maxValue;
    public float minValue;
    public string displayName;
    public string group;

    private const string DEFAULT_GROUP = "Default";

    public TweakableMemberAttribute()
        : this(0, 100)
    {
    }

    public TweakableMemberAttribute(string displayname, string group = DEFAULT_GROUP)
        : this(0, 100, displayname, group)
    {
    }

    public TweakableMemberAttribute(float minValue, float maxValue, string displayName = "", string group = DEFAULT_GROUP)
    {
        this.minValue = minValue;
        this.maxValue = maxValue;
        this.displayName = displayName;
        this.group = group;
    }

}