// UN-HUMANITY — primes a placed SPUM unit's animator and starts its idle loop
// at runtime. SpumCast adds this when a unit is placed at scene-build time
// (edit mode stays side-effect-free; the baked sprites already show).

using UnityEngine;

public class SpumIdle : MonoBehaviour
{
    void Start() => SpumCast.InitAndIdle(gameObject);
}
