using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicPosing : MonoBehaviour
{
    [System.Serializable]
    public class FingerBones
    {
        public Transform[] baseBones, secondaryBones, tipBones, fingerTips;
        [HideInInspector] public List<Quaternion> baseBonesStart = new List<Quaternion>(), secondaryBonesStart = new List<Quaternion>(), tipBonesStart = new List<Quaternion>();
        [HideInInspector] public List<bool> isInteractingBase = new List<bool>(), isInteractingSecondary = new List<bool>(), isInteractingTip = new List<bool>();
        [HideInInspector] public List<Quaternion> calculatedRotations = new List<Quaternion>();
    }

    public FingerBones fingerBones;

    [System.Serializable]
    public class FingerTargets
    {
        public Transform[] baseGrippedBones, secondaryGrippedBones, tipGrippedBones;
    }

    public FingerTargets fingerTargets;

    public float checkForObjectRadius;
    private bool isGripping;
    public float fingerPoseTime;
    public Transform[] targetBones;
    private GameObject objectInteracting;
    private Vector3 lastPosition;
    private Vector3 lastScale;
    private Quaternion lastRotation;
    private bool isMoving;

    void Start()
    {
        InitializeFingerBones();
    }

    void InitializeFingerBones()
    {
        for (int i = 0; i < fingerBones.baseBones.Length; i++)
        {
            fingerBones.baseBonesStart.Add(fingerBones.baseBones[i].localRotation);
            fingerBones.isInteractingBase.Add(false);
        }

        for (int i = 0; i < fingerBones.secondaryBones.Length; i++)
        {
            fingerBones.secondaryBonesStart.Add(fingerBones.secondaryBones[i].localRotation);
            fingerBones.isInteractingSecondary.Add(false);
        }

        for (int i = 0; i < fingerBones.tipBones.Length; i++)
        {
            fingerBones.tipBonesStart.Add(fingerBones.tipBones[i].localRotation);
            fingerBones.isInteractingTip.Add(false);
        }
    }

    void Update()
    {
        if (Physics.CheckSphere(transform.position, checkForObjectRadius) && !isGripping)
        {
            objectInteracting = Physics.OverlapSphere(transform.position, checkForObjectRadius)[0].gameObject;
            if (lastPosition != objectInteracting.transform.position || lastRotation != objectInteracting.transform.rotation || lastScale != objectInteracting.transform.lossyScale)
            {
                isMoving = true;
            }
            else
            {
                isMoving = false;
            }
            lastPosition = objectInteracting.transform.position;
            lastScale = objectInteracting.transform.lossyScale;
            lastRotation= objectInteracting.transform.rotation;
            if(isMoving)
            {
                StartCoroutine(Grip());
            }
        }
        else
        {
            objectInteracting = null;
        }
        CheckForInteraction();
    }

    void CheckForInteraction()
    {
        for (int i = 0; i < fingerBones.secondaryBones.Length; i++)
        {
            Vector3 direction = fingerBones.secondaryBones[i].position - fingerBones.baseBones[i].position;
            float distance = direction.magnitude;
            direction.Normalize();

            if (Physics.SphereCast(fingerBones.baseBones[i].position, 0.01f, direction, out RaycastHit hit, distance) || Physics.CheckSphere(fingerBones.secondaryBones[i].position, 0.02f))
            {
                fingerBones.isInteractingBase[i] = true;
            }
        }

        for (int i = 0; i < fingerBones.tipBones.Length; i++)
        {
            Vector3 direction = fingerBones.tipBones[i].position - fingerBones.secondaryBones[i].position;
            float distance = direction.magnitude;
            direction.Normalize();

            if (Physics.SphereCast(fingerBones.secondaryBones[i].position, 0.015f, direction, out RaycastHit hit, distance) || Physics.CheckSphere(fingerBones.fingerTips[i].position, 0.01f) || Physics.CheckSphere(fingerBones.secondaryBones[i].position, 0.01f) || Physics.CheckSphere(fingerBones.tipBones[i].position, 0.01f))
            {
                fingerBones.isInteractingSecondary[i] = true;
            }
        }

        for (int i = 0; i < fingerBones.fingerTips.Length; i++)
        {
            if (Physics.CheckSphere(fingerBones.fingerTips[i].position, 0.01f))
            {
                fingerBones.isInteractingTip[i] = true;
            }
        }
    }

    IEnumerator Grip()
    {
        isGripping = true;

        yield return PoseFingers(fingerBones.baseBones, fingerTargets.baseGrippedBones, fingerBones.isInteractingBase);
        yield return PoseFingers(fingerBones.secondaryBones, fingerTargets.secondaryGrippedBones, fingerBones.isInteractingSecondary);
        yield return PoseFingers(fingerBones.tipBones, fingerTargets.tipGrippedBones, fingerBones.isInteractingTip);

        SaveCalculatedRotations();
        MapToTarget();
        ResetGrip();
    }

    IEnumerator PoseFingers(Transform[] bones, Transform[] targets, List<bool> isInteracting)
    {
        float timer = 0;

        while (timer < fingerPoseTime)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (!isInteracting[i])
                {
                    bones[i].localRotation = Quaternion.Slerp(bones[i].localRotation, targets[i].localRotation, timer / fingerPoseTime);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }
    }

    void SaveCalculatedRotations()
    {
        fingerBones.calculatedRotations.Clear();

        foreach (var bone in fingerBones.baseBones)
        {
            fingerBones.calculatedRotations.Add(bone.localRotation);
        }

        foreach (var bone in fingerBones.secondaryBones)
        {
            fingerBones.calculatedRotations.Add(bone.localRotation);
        }

        foreach (var bone in fingerBones.tipBones)
        {
            fingerBones.calculatedRotations.Add(bone.localRotation);
        }
    }

    void MapToTarget()
    {
        int index = 0;
        for (int i = 0; i < fingerBones.baseBones.Length; i++)
        {
            targetBones[index++].localRotation = fingerBones.calculatedRotations[i];
        }

        for (int i = 0; i < fingerBones.secondaryBones.Length; i++)
        {
            targetBones[index++].localRotation = fingerBones.calculatedRotations[fingerBones.baseBones.Length + i];
        }

        for (int i = 0; i < fingerBones.tipBones.Length; i++)
        {
            targetBones[index++].localRotation = fingerBones.calculatedRotations[fingerBones.baseBones.Length + fingerBones.secondaryBones.Length + i];
        }
    }

    void ResetGrip()
    {
        for (int i = 0; i < fingerBones.baseBones.Length; i++)
        {
            fingerBones.baseBones[i].localRotation = fingerBones.baseBonesStart[i];
            fingerBones.isInteractingBase[i] = false;
        }

        for (int i = 0; i < fingerBones.secondaryBones.Length; i++)
        {
            fingerBones.secondaryBones[i].localRotation = fingerBones.secondaryBonesStart[i];
            fingerBones.isInteractingSecondary[i] = false;
        }

        for (int i = 0; i < fingerBones.tipBones.Length; i++)
        {
            fingerBones.tipBones[i].localRotation = fingerBones.tipBonesStart[i];
            fingerBones.isInteractingTip[i] = false;
        }

        isGripping = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, checkForObjectRadius);
    }
}
