using UnityEngine;
using UnityEngine.AI;

public static class Utility
{
    public static Vector3 GetRandomPointOnNavMesh(Vector3 center, float distance, int areaMask)
    {
        var randomPos = Random.insideUnitSphere * distance + center; // 반지름을 기준으로 원을 그려서 해당 범위를 구한다.
        
        NavMeshHit hit;
        
        NavMesh.SamplePosition(randomPos, out hit, distance, areaMask); // 해당 원안에서 랜덤한 위치를 NavMeshHit Hit 로 반환 한다.
        
        return hit.position;
    }
    
    public static float GedRandomNormalDistribution(float mean, float standard)
    {
        var x1 = Random.Range(0f, 1f);
        var x2 = Random.Range(0f, 1f);
        return mean + standard * (Mathf.Sqrt(-2.0f * Mathf.Log(x1)) * Mathf.Sin(2.0f * Mathf.PI * x2));
    }
}