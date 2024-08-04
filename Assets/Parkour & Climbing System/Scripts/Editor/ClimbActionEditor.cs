using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FC_ParkourSystem
{
    [CustomEditor(typeof(ClimbActions))]
    public class ClimbActionEditor : Editor
    {
        bool leftHand;
        bool rightHand;
        bool rightFoot;
        bool leftFoot;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var action = target as ClimbActions;

            if (action.IKEnabled)
            {
                leftHand = Field(action.leftHand, leftHand, "Left Hand");
                leftFoot = Field(action.leftFoot, leftFoot, "Left Foot");
                rightHand = Field(action.rightHand, rightHand, "Right Hand");
                rightFoot = Field(action.rightFoot, rightFoot, "Right Foot");
            }
        }

        bool Field(IkPart ikPart, bool ik, string fieldName)
        {
            ik = EditorGUILayout.Foldout(ik, fieldName);
            if (ik)
            {
                EditorGUI.indentLevel++;
                ikPart.ikStartTime = EditorGUILayout.FloatField("IkStartTime", ikPart.ikStartTime);
                ikPart.ikEndTime = EditorGUILayout.FloatField("IkEndTime", ikPart.ikEndTime);
                EditorGUI.indentLevel--;
            }
            return ik;
        }
    }
}
