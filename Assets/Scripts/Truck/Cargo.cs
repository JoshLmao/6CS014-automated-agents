public class Cargo
{
    /// <summary>
    /// Maximum amount of packages a truck can carry in it's cargo
    /// </summary>
    public static int MAX_PACKAGES = 15;

    private int m_packageCount = 0;
    /// <summary>
    /// Amount of packages
    /// </summary>
    public int PackageCount
    {
        get { return m_packageCount; }
    }

    /// <summary>
    /// Adds the amount of packages to the Cargo. Won't add if at MAX_PACKAGES capacity
    /// </summary>
    /// <param name="amount"></param>
    public void AddPackages(int amount)
    {
        if (m_packageCount + amount > MAX_PACKAGES)
        {
            m_packageCount = MAX_PACKAGES;
        }
        else
        {
            m_packageCount += amount;
        }
    }

    /// <summary>
    /// Removes the amount of packages from the cargo. Won't remove if no packages remain
    /// </summary>
    /// <param name="amount"></param>
    public void RemovePackages(int amount)
    {
        if (PackageCount - amount >= 0)
        {
            m_packageCount -= amount;
        }
        else
        {
            m_packageCount = 0;
        }
    }
}
