using DIASMedicalJournal.Data;
using DIASMedicalJournal.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// ----------------------------------------------------------------------------------------
// Creates database if it doesn't already exist
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();

    // Check if data already exists, if not it creates
    if (!context.Doctors.Any())
    {
        context.Doctors.AddRange(
            new Doctor { Name = "Dr. Test", Department = "Cardiology" },
            new Doctor { Name = "Dr. Test2", Department = "Neurology" },
            new Doctor { Name = "Dr. Test3", Department = "Pediatrics" }
        );
        context.SaveChanges();
    }

    if (!context.Patients.Any())
    {
        context.Patients.AddRange(
            new Patient { Name = "Sir Test McTest", SocialSecurityNumber = "1234567890" },
            new Patient { Name = "Mrs Test McTest", SocialSecurityNumber = "2345678901" },
            new Patient { Name = "Mr Test", SocialSecurityNumber = "3456789012" }
        );
        context.SaveChanges();
    }

    // ----------------------------------------------------------------------------------------
    // Add Admissions if they don't exist
    if (!context.Admissions.Any())
    {
        context.Admissions.AddRange(
             new Admission
             {
                 Department = "Cardiology",
                 Doctors = context.Doctors.Where(d => d.Department == "Cardiology").ToList(),
                 MedicalJournal = context.Patients.First(p => p.SocialSecurityNumber == "1234567890")
             },
            new Admission
            {
                Department = "Neurology",
                Doctors = context.Doctors.Where(d => d.Department == "Neurology").ToList(),
                MedicalJournal = context.Patients.First(p => p.SocialSecurityNumber == "2345678901")
            },
            new Admission
            {
                Department = "Pediatrics",
                Doctors = context.Doctors.Where(d => d.Department == "Pediatrics").ToList(),
                MedicalJournal = context.Patients.First(p => p.SocialSecurityNumber == "3456789012")
            }
        );
        context.SaveChanges();
    }
}

// ----------------------------------------------------------------------------------------
// Create new patient
app.MapPost("/Patients", async (Patient patient, AppDbContext db) =>
{
    db.Patients.Add(patient);
    await db.SaveChangesAsync();

    return Results.Ok();
});

// ----------------------------------------------------------------------------------------
// Create new doctor
app.MapPost("/Doctors", async (Doctor doctor, AppDbContext db) =>
{
    db.Doctors.Add(doctor);
    await db.SaveChangesAsync();

    return Results.Ok();
});

// ----------------------------------------------------------------------------------------
// Check if doctor X has access to patient Y
app.MapGet("/accessPatient", async (int patientID, int doctorID, AppDbContext db) =>
{
    var patientAdmission = await db.Admissions
        .Include(a => a.Doctors)
        .Include(a => a.MedicalJournal)
        .FirstOrDefaultAsync(a => a.MedicalJournal.PatientId == patientID);

    if (patientAdmission == null)
    {
        return Results.NotFound("Patient not found.");
    }

    var doctor = await db.Doctors.FindAsync(doctorID);
    if (doctor == null)
    {
        return Results.NotFound("Doctor not found.");
    }

    // bool to check if doctor has access
    bool doctorHasAccess = patientAdmission.Doctors.Any(d => d.DoctorId == doctorID) &&
                           patientAdmission.Department == doctor.Department;

    if (doctorHasAccess)
    {
        var response = new
        {
            Message = "Doctor has access to the patient's medical journal.",
            MedicalJournal = patientAdmission.MedicalJournal
        };
        return Results.Ok(response);
    }
    else
    {
        return Results.Json(new { message = "Doctor does not have access to the patient's medical journal." }, statusCode: StatusCodes.Status403Forbidden);
    }
});

// ----------------------------------------------------------------------------------------
// Assign patient to doctor
app.MapPost("/admissions/{admissionId}/doctors/{doctorId}", async (int admissionId, int doctorId, AppDbContext db) =>
{
    var admission = await db.Admissions.Include(a => a.Doctors).FirstOrDefaultAsync(a => a.AdmissionId == admissionId);
    var doctor = await db.Doctors.FindAsync(doctorId);

    if (admission == null || doctor == null)
    {
        return Results.NotFound("Admission or doctor not found.");
    }

    // Check if the doctor is already added to this admission
    if (admission.Doctors.Any(d => d.DoctorId == doctorId))
    {
        return Results.BadRequest("Doctor is already assigned to this admission.");
    }

    admission.Doctors.Add(doctor);
    await db.SaveChangesAsync();

    return Results.Ok(admission);
});


// ----------------------------------------------------------------------------------------
// Get a list of all patients for a doctor
app.MapGet("/allPatientsForDoc", async (int doctorID, AppDbContext db) =>
{
    var admissions = await db.Admissions
        .Include(a => a.Doctors)
        .Include(a => a.MedicalJournal)
        .Where(a => a.Doctors.Any(d => d.DoctorId == doctorID))
        .Select(a => a.MedicalJournal)
        .ToListAsync();

    return Results.Ok(admissions);
});

// ----------------------------------------------------------------------------------------
// Get a list of all doctors for a patient
app.MapGet("/allDocForPatient", async (int patientID, AppDbContext db) =>
{
    var admission = await db.Admissions
        .Include(a => a.Doctors)
        .Include(a => a.MedicalJournal)
        .FirstOrDefaultAsync(a => a.MedicalJournal.PatientId == patientID);

    if (admission == null)
    {
        return Results.NotFound("Patient not found.");
    }

    return Results.Ok(admission.Doctors);
});


app.MapGet("/Patients", async (AppDbContext db) => await db.Patients.ToListAsync());
app.MapGet("/Admissions", async(AppDbContext db) => await db.Admissions.ToListAsync());
app.MapGet("/Doctors", async (AppDbContext db) => await db.Doctors.ToListAsync());

app.MapControllers();

app.Run();
