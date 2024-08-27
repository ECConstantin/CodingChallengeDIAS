namespace DIASMedicalJournal.Models
{
    public class Admission
    {
        public int AdmissionId { get; set; }
        public string Department { get; set; }
        public List<Doctor> Doctors { get; set; } = new List<Doctor>();
        public Patient MedicalJournal { get; set; }
    }

}
