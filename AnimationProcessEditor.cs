using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Spine.Unity;
using System.Reflection;
using System.ComponentModel;

public class AnimationProcessEditor : EditorWindow
{
    [MenuItem("LevelEditor/Window")]
    static void AddWindow()
    {
        //创建窗口
        Rect wr = new Rect(0, 0, 960, 540);
        AnimationProcessEditor window = (AnimationProcessEditor)EditorWindow.GetWindowWithRect(typeof(AnimationProcessEditor), wr, true, "阿U－关卡编辑器");
        window.Show();

        window.Initialize(@"Assets/UGame/LeSiMath/ls_level_4/Animation");
    }

    public enum ProcessType
    {
        [Description("动画")]
        PT_ANIMATION = 0,
        [Description("声音")]
        PT_SOUND = 1,
        PT_COUNT,
    }

    public static string FetchDescription(ProcessType value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString());
        DescriptionAttribute[] attributes =
              (DescriptionAttribute[])fi.GetCustomAttributes(
              typeof(DescriptionAttribute), false);
        return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
    }

    public class Process
    {
        public string desc;
        public ProcessType eProcessType = ProcessType.PT_ANIMATION;
        public string assetPath;


        public object binderObject;
        public string fileName;
        public string showName;
        public string actionName;
        public float length;
        public float actionForward;
        public float actionDelay;
        public bool actionLoop;
        public bool actionTimeScale;

        public Spine.Animation getAction()
        {
            if(eProcessType != ProcessType.PT_ANIMATION)
            {
                return null;
            }

            var role = binderObject as Role;
            if(null == role)
            {
                return null;
            }

            if(string.IsNullOrEmpty(actionName))
            {
                return null;
            }

            Spine.Animation action = null;
            var animationState = role.animation.skeletonDataAsset.GetAnimationStateData();
            if (null != animationState)
            {
                var animations = animationState.SkeletonData.Animations;
                if(null != animations && null != animations.Items)
                {
                    for(int i = 0; i < animations.Items.Length; ++i)
                    {
                        var item = animations.Items[i];
                        if(null != item && string.Equals(item.Name, actionName))
                        {
                            action = item;
                            break;
                        }
                    }
                }
            }

            return action;
        }
    }

    public class Role
    {
        public GameObject goRole;
        public SkeletonAnimation animation;
        public string assetPath;
        public Spine.AnimationStateData stateData;
        public Spine.ExposedList<Spine.Animation> animations;
        public string fileName;
        public string showName;

        public void OnDestroy()
        {
            animation = null;
            assetPath = string.Empty;
            stateData = null;
            animations.Clear();
            Object.DestroyImmediate(goRole);
            goRole = null;
        }

        public void Initialize()
        {
            if (null != animation)
            {
                animation.Initialize(false);
            }
        }

        public void PrintAnimationName()
        {
            if(null != animations)
            {
                for (int j = 0; j < animations.Count; ++j)
                {
                    var animation = animations.Items[j];
                    if (null != animation)
                    {
                        Debug.LogFormat("animation = <color=#00ff00>[{0}][{1}]</color>", animation.Name,animation.Duration);
                    }
                }
            }
        }
    }

    protected Dictionary<string, Role> mRoleDics = new Dictionary<string, Role>();
    protected string mAnimationPath = string.Empty;
    protected List<Process> mProcess = new List<Process>(32);

    protected void Initialize(string animationPath)
    {
        mAnimationPath = animationPath;
        mRoleDics.Clear();
        mProcess.Clear();
        CreateSpineAnimationObjects(mAnimationPath);
    }

    protected void OnSelectProcessTypeChanged(object value)
    {
        var argv = value as object[];
        Process process = argv[0] as Process;
        process.eProcessType = (ProcessType)argv[1];
    }

    protected void OnProcessTitleGUI(int index)
    {
        GUI.color = Color.cyan;
        EditorGUILayout.LabelField(string.Format("[流程步骤:[{0}]]", index + 1), GUILayout.MaxWidth(100));
        GUI.color = Color.white;
    }

    protected void OnProcessTypeGUI(Process process)
    {
        GUI.color = Color.green;
        EditorGUILayout.LabelField("[流程类型]:", GUILayout.MaxWidth(60));
        GUI.color = Color.white;
        if (GUILayout.Button(FetchDescription(process.eProcessType), "GV Gizmo DropDown", GUILayout.MaxWidth(80)))
        {
            GenericMenu menu = new GenericMenu();
            for (int j = 0; j < (int)ProcessType.PT_COUNT; ++j)
            {
                ProcessType eProcessType = (ProcessType)j;
                menu.AddItem(new GUIContent(FetchDescription(eProcessType)), process.eProcessType == eProcessType, OnSelectProcessTypeChanged, new object[]
                    {
                            process,
                            eProcessType,
                    });
            }
            menu.ShowAsContext();
        }
        GUI.color = Color.white;
    }

    protected void OnRoleTypeSelected(object value)
    {
        var argv = value as object[];
        Process process = argv[0] as Process;
        Role role = argv[1] as Role;
        process.binderObject = role;
        process.assetPath = role.assetPath;
        process.fileName = role.fileName;

        var endName = @"_SkeletonData.asset";
        var fileNameWithExt = System.IO.Path.GetFileName(role.assetPath);
        process.showName = fileNameWithExt.Substring(0, fileNameWithExt.Length - endName.Length);
    }

    protected void OnRoleActionSelected(object value)
    {
        var argv = value as object[];
        Process process = argv[0] as Process;
        process.actionName = argv[1] as string;
    }

    protected void OnProcessAnimationGUI(Process process)
    {
        GUI.color = Color.green;
        EditorGUILayout.LabelField("[角色类型]:", GUILayout.MaxWidth(60));
        GUI.color = Color.white;
        var content = string.IsNullOrEmpty(process.showName) ? "Invalid Role" : process.showName;
        if (GUILayout.Button(content, "GV Gizmo DropDown", GUILayout.MaxWidth(80)))
        {
            GenericMenu menu = new GenericMenu();
            var iter = mRoleDics.GetEnumerator();
            while (iter.MoveNext())
            {
                var role = iter.Current.Value;
                menu.AddItem(new GUIContent(role.showName), role.fileName.Equals(process.fileName), OnRoleTypeSelected, new object[]
                    {
                    process,role
                    });
            }
            menu.ShowAsContext();
        }
    }

    protected void OnProcessActionGUI(Process process)
    {
        var role = process.binderObject as Role;
        if(null != role && process.eProcessType == ProcessType.PT_ANIMATION)
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField("[角色动作]:", GUILayout.MaxWidth(60));
            GUI.color = Color.white;
            bool isActionValid = !string.IsNullOrEmpty(process.actionName);

            Spine.Animation action = null;
            if (isActionValid)
            {
                action = process.getAction();
            }
            isActionValid = null != action;

            var content = isActionValid ? process.actionName : "Invalid Action";
            if (GUILayout.Button(content, "GV Gizmo DropDown", GUILayout.MaxWidth(80)))
            {
                if(null != role.animations && null != role.animations.Items)
                {
                    GenericMenu menu = new GenericMenu();
                    for (int i = 0; i < role.animations.Items.Length; ++i)
                    {
                        var actionName = role.animations.Items[i].Name;
                        menu.AddItem(new GUIContent(actionName), string.Equals(actionName, process.actionName), OnRoleActionSelected, new object[]
                        {
                            process,actionName
                        });
                    }
                    menu.ShowAsContext();
                }
            }

            if (null != action)
            {
                GUI.color = Color.magenta;
                var lengthDesc = string.Format("[时长:{0:F3}秒]", action.Duration);
                EditorGUILayout.LabelField(lengthDesc, GUILayout.Width(100));
                GUI.color = Color.white;
            }
        }
    }

    void OnGUI()
    {
        if (GUILayout.Button("创建流程", GUILayout.Width(100)))
        {
            mProcess.Add(new Process());
        }

        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < mProcess.Count; ++i)
        {
            GUILayout.Space(6);
            var process = mProcess[i];
            EditorGUILayout.BeginVertical();
            OnProcessTitleGUI(i);
            EditorGUILayout.BeginHorizontal();
            OnProcessTypeGUI(process);
            if (process.eProcessType == ProcessType.PT_ANIMATION)
            {
                OnProcessAnimationGUI(process);
                OnProcessActionGUI(process);

                var action = process.getAction();
                if(null != action)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("是否循环?", GUILayout.Width(60));
                    GUI.color = Color.white;
                    process.actionLoop = EditorGUILayout.Toggle(process.actionLoop, GUILayout.Width(60));
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField(string.Format("延时{0:F3}秒执行:",process.actionDelay), GUILayout.Width(80));
                    GUI.color = Color.white;
                    process.actionDelay = EditorGUILayout.FloatField(process.actionDelay, GUILayout.Width(60));
                }
                
            }
            else if (process.eProcessType == ProcessType.PT_SOUND)
            {

            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        if (EditorGUI.EndChangeCheck())
        {

        }
    }

    void CreateSpineAnimationObjects(string folder)
    {
        var folders = new string[] { folder };
        var guids = AssetDatabase.FindAssets(string.Empty, folders);
        var endName = @"_SkeletonData.asset";
        for (int i = 0; i < guids.Length; ++i)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if(path.EndsWith(endName))
            {
                //Debug.LogErrorFormat("[guid]:{0} [path]:{1}", guids[i], path);
                var gameObject = new GameObject("skeleton_" + i,typeof(MeshRenderer), typeof(SkeletonAnimation));
                SkeletonAnimation ani = gameObject.GetComponent<SkeletonAnimation>();
                ani.skeletonDataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
                var stateData = ani.skeletonDataAsset.GetAnimationStateData();
                var animations = stateData.SkeletonData.Animations;
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                var fileNameWithExtention = System.IO.Path.GetFileName(path);
                if (!mRoleDics.ContainsKey(guids[i]))
                {
                    mRoleDics.Add(guids[i], new Role
                    {
                        goRole = gameObject,
                        animation = ani,
                        assetPath = path,
                        stateData = stateData,
                        animations = animations,
                        fileName = fileName,
                        showName = fileNameWithExtention.Substring(0, fileNameWithExtention.Length - endName.Length),
                    });

                    mRoleDics[guids[i]].Initialize();
                }
            }
        }
    }
    void DestroySpineAnimationObjects()
    {
        var iter = mRoleDics.GetEnumerator();
        while(iter.MoveNext())
        {
            iter.Current.Value.OnDestroy();
        }
        mRoleDics.Clear();
    }

    void OnDestroy()
    {
        DestroySpineAnimationObjects();
        mProcess.Clear();
    }
}
