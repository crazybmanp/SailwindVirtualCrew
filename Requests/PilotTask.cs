namespace SailwindVirtualCrew
{
    public class PilotTask
    {
        public Crewman AssignedCrewman { get; }

        public PilotTask(Crewman crewman)
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
