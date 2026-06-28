
public class GroundHeight
{
    RoadCollider roadCollider;
    Terrain terrain;

    public GroundHeight(RoadCollider roadCollider, Terrain terrain)
    {
        this.roadCollider = roadCollider;
        this.terrain = terrain;
    }

    public float GetHeight(float x, float z)
    {
        if (roadCollider.TryGetHeight(x, z, out var height))
        {
            return height;
        }
        return terrain.GetHeight(x, z);
    }
}