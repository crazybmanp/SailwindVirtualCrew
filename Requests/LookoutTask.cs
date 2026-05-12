namespace SailwindVirtualCrew
{
    public class LookoutTask
    {
        public Crewman AssignedCrewman { get; }

        public LookoutTask(Crewman crewman)
        {
            AssignedCrewman     = crewman;
            crewman.CurrentTask = this;
        }

        public void Cancel()
        {
            AssignedCrewman.CurrentTask = null;
        }
    }
}
