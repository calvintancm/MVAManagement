-- ═══════════════════════════════════════════════════════════════════════════
-- LegalCase MVA — Seed Data Script (Sections 6–10 FIXED)
-- Run AFTER the main seed script sections 1–5 have already run successfully
-- OR run the complete script below which includes ALL sections 1–12
-- ═══════════════════════════════════════════════════════════════════════════

USE MVAManagement;
GO

SET NOCOUNT ON;
PRINT '═══════════════════════════════════════════════════════════';
PRINT ' LegalCase MVA — Seed Script (Sections 6–10 Fixed)';
PRINT ' Started: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '═══════════════════════════════════════════════════════════';

-- ═══════════════════════════════════════════════════════════════════════════
-- NOTE: Variables must be declared in the SAME batch (before any GO).
--       All sections below are in one batch — no GO between them.
-- ═══════════════════════════════════════════════════════════════════════════

-- ── Collect all FK IDs upfront ────────────────────────────────────────────
DECLARE @VenueKL    INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MTKL');
DECLARE @VenueSHA   INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MTSHA');
DECLARE @VenueJB    INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MSJB');
DECLARE @VenueKLSes INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MSKL');

DECLARE @CW1 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'ahmad.farhan');
DECLARE @CW2 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'siti.norzahra');
DECLARE @CW3 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'rajendran.k');
DECLARE @CW4 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'lim.swee.ling');
DECLARE @CW5 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'nurul.huda');
DECLARE @CW6 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'tan.boon.huat');

DECLARE @CaseF1  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2024-0001');
DECLARE @CaseF2  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2024-0002');
DECLARE @CaseF3  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2023-0047');
DECLARE @CaseF4  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2023-0051');
DECLARE @CaseF8  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2024-0003');
DECLARE @CaseF11 INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2024-0004');
DECLARE @CaseF12 INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2023-0078');

DECLARE @M1 DATE = CAST(DATEADD(MONTH, -1, GETDATE()) AS DATE);
DECLARE @M2 DATE = CAST(DATEADD(MONTH, -2, GETDATE()) AS DATE);
DECLARE @M3 DATE = CAST(DATEADD(MONTH, -3, GETDATE()) AS DATE);

-- ── Validate IDs loaded ───────────────────────────────────────────────────
IF @CaseF1 IS NULL OR @CaseF2 IS NULL OR @CaseF3 IS NULL
BEGIN
    PRINT 'ERROR: CaseFile IDs not found. Run sections 1-5 first.';
    RETURN;
END

-- ═══════════════════════════════════════════════════════════════════════════
-- 6. HEARING RECORDS
-- Model columns: CaseFileId, HearingStageId, CourtVenueId, ScheduledDate,
--                HearingTime, CourtroomNumber, PresidingJudge,
--                IsCompleted, HearingOutcome, AdjournedToDate,
--                ProgressNotes, CreatedAt
-- NOTE: HearingRecord requires HearingStageId (FK to HearingStage table)
--       We use 0 as placeholder — update to real HearingStage IDs after
--       seeding that table, or create HearingStage rows here first.
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 6 ] Seeding HearingRecord...';

-- Seed HearingStage lookup if the table exists and is empty
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HearingStage')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM HearingStage WHERE StageCode = 'MENTI')
        INSERT INTO HearingStage (StageCode, StageDescription, DisplayOrder)
        VALUES ('MENTI', 'Mention', 1);

    IF NOT EXISTS (SELECT 1 FROM HearingStage WHERE StageCode = 'CMC')
        INSERT INTO HearingStage (StageCode, StageDescription, DisplayOrder)
        VALUES ('CMC', 'Case Management Conference', 2);

    IF NOT EXISTS (SELECT 1 FROM HearingStage WHERE StageCode = 'INTER')
        INSERT INTO HearingStage (StageCode, StageDescription, DisplayOrder)
        VALUES ('INTER', 'Interlocutory Application', 3);

    IF NOT EXISTS (SELECT 1 FROM HearingStage WHERE StageCode = 'TRIAL')
        INSERT INTO HearingStage (StageCode, StageDescription, DisplayOrder)
        VALUES ('TRIAL', 'Full Trial', 4);

    IF NOT EXISTS (SELECT 1 FROM HearingStage WHERE StageCode = 'MEDIA')
        INSERT INTO HearingStage (StageCode, StageDescription, DisplayOrder)
        VALUES ('MEDIA', 'Mediation', 5);

    IF NOT EXISTS (SELECT 1 FROM HearingStage WHERE StageCode = 'APPEA')
        INSERT INTO HearingStage (StageCode, StageDescription, DisplayOrder)
        VALUES ('APPEA', 'Appeal', 6);

    PRINT '   HearingStage: OK';
END

DECLARE @StgMention INT = (SELECT Id FROM HearingStage WHERE StageCode = 'MENTI');
DECLARE @StgCMC     INT = (SELECT Id FROM HearingStage WHERE StageCode = 'CMC');
DECLARE @StgTrial   INT = (SELECT Id FROM HearingStage WHERE StageCode = 'TRIAL');
DECLARE @StgMedia   INT = (SELECT Id FROM HearingStage WHERE StageCode = 'MEDIA');

-- Case 3: Upcoming CMC in 12 days
IF NOT EXISTS (SELECT 1 FROM HearingRecord WHERE CaseFileId = @CaseF3 AND IsCompleted = 0)
    INSERT INTO HearingRecord
        (CaseFileId, HearingStageId, CourtVenueId, ScheduledDate,
         HearingTime, CourtroomNumber, PresidingJudge,
         IsCompleted, HearingOutcome, ProgressNotes, CreatedAt)
    VALUES
        (@CaseF3, @StgCMC, @VenueKL,
         CAST(DATEADD(DAY, 12, GETDATE()) AS DATE),
         '09:00', 'Courtroom 5A', 'YA Dato'' Roslan bin Ibrahim',
         0, NULL,
         'Pre-trial case management conference. Both parties to file and exchange witness statements.',
         GETDATE());

-- Case 4: Upcoming Full Trial in 2 days (URGENT)
IF NOT EXISTS (SELECT 1 FROM HearingRecord WHERE CaseFileId = @CaseF4 AND IsCompleted = 0)
    INSERT INTO HearingRecord
        (CaseFileId, HearingStageId, CourtVenueId, ScheduledDate,
         HearingTime, CourtroomNumber, PresidingJudge,
         IsCompleted, HearingOutcome, ProgressNotes, CreatedAt)
    VALUES
        (@CaseF4, @StgTrial, @VenueSHA,
         CAST(DATEADD(DAY, 2, GETDATE()) AS DATE),
         '09:00', 'Courtroom 3', 'YA Puan Sri Noraini binti Ahmad',
         0, NULL,
         'Day 1 of full trial. Plaintiff to call 3 witnesses. Medical expert booked at 10:30am.',
         GETDATE());

-- Case 12: Upcoming Mention in 8 days (soon)
IF NOT EXISTS (SELECT 1 FROM HearingRecord WHERE CaseFileId = @CaseF12 AND IsCompleted = 0)
    INSERT INTO HearingRecord
        (CaseFileId, HearingStageId, CourtVenueId, ScheduledDate,
         HearingTime, CourtroomNumber, PresidingJudge,
         IsCompleted, HearingOutcome, ProgressNotes, CreatedAt)
    VALUES
        (@CaseF12, @StgMention, @VenueJB,
         CAST(DATEADD(DAY, 8, GETDATE()) AS DATE),
         '10:00', 'Courtroom 2B', 'YA Tuan Mohd Zaki bin Salleh',
         0, NULL,
         'Mention for case management directions. Defendant counsel to confirm trial dates.',
         GETDATE());

-- Case 1: Upcoming Mediation in 20 days
IF NOT EXISTS (SELECT 1 FROM HearingRecord WHERE CaseFileId = @CaseF1 AND IsCompleted = 0)
    INSERT INTO HearingRecord
        (CaseFileId, HearingStageId, CourtVenueId, ScheduledDate,
         HearingTime, CourtroomNumber, PresidingJudge,
         IsCompleted, HearingOutcome, ProgressNotes, CreatedAt)
    VALUES
        (@CaseF1, @StgMedia, @VenueKLSes,
         CAST(DATEADD(DAY, 20, GETDATE()) AS DATE),
         '14:00', 'Mediation Room 1', NULL,
         0, NULL,
         'Court-directed mediation. Both parties to prepare settlement position paper beforehand.',
         GETDATE());

-- Case 3: Past completed Mention (2 months ago)
IF NOT EXISTS (SELECT 1 FROM HearingRecord WHERE CaseFileId = @CaseF3 AND IsCompleted = 1)
    INSERT INTO HearingRecord
        (CaseFileId, HearingStageId, CourtVenueId, ScheduledDate,
         HearingTime, CourtroomNumber, PresidingJudge,
         IsCompleted, HearingOutcome, AdjournedToDate, ProgressNotes, CreatedAt)
    VALUES
        (@CaseF3, @StgMention, @VenueKL,
         CAST(DATEADD(MONTH, -2, GETDATE()) AS DATE),
         '09:00', 'Courtroom 5A', 'YA Dato'' Roslan bin Ibrahim',
         1,
         'Adjourned to next mention for case management conference.',
         CAST(DATEADD(DAY, 12, GETDATE()) AS DATE),
         'First mention. Defendant filed statement of defence. Trial directions sought.',
         DATEADD(MONTH, -3, GETDATE()));

PRINT '   HearingRecord: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 7. INJURY RECORDS
-- Model columns: CaseFileId, InjuryDescription, BodyPart, SeverityLevel,
--                IsPermanentDisability, DisabilityPercentage,
--                TreatmentStatus, HospitalName, DoctorName, MedicalSpecialty,
--                MedicalReportStatus, MedicalReportReceivedDate
-- NOTE: No CreatedAt on InjuryRecord model — removed
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 7 ] Seeding InjuryRecord...';

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF1)
    INSERT INTO InjuryRecord
        (CaseFileId, InjuryDescription, BodyPart, SeverityLevel,
         IsPermanentDisability, DisabilityPercentage,
         TreatmentStatus, HospitalName, DoctorName, MedicalSpecialty,
         MedicalReportStatus, MedicalReportReceivedDate)
    VALUES
        (@CaseF1, 'Cervical Spine Strain (Whiplash)', 'Neck / Cervical Spine', 'Moderate',
         0, NULL,
         'Discharged', 'Hospital Kuala Lumpur', 'Dr. Azman bin Yusof', 'Orthopaedic',
         'Received', '2024-02-10');

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF2)
    INSERT INTO InjuryRecord
        (CaseFileId, InjuryDescription, BodyPart, SeverityLevel,
         IsPermanentDisability, DisabilityPercentage,
         TreatmentStatus, HospitalName, DoctorName, MedicalSpecialty,
         MedicalReportStatus, MedicalReportReceivedDate)
    VALUES
        (@CaseF2, 'Compound Fracture of Left Radius', 'Left Arm / Radius', 'Severe',
         0, NULL,
         'Discharged', 'Pantai Hospital Kuala Lumpur', 'Dr. Selvarajah a/l Krishnan', 'Orthopaedic',
         'Received', '2024-01-05');

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF3)
    INSERT INTO InjuryRecord
        (CaseFileId, InjuryDescription, BodyPart, SeverityLevel,
         IsPermanentDisability, DisabilityPercentage,
         TreatmentStatus, HospitalName, DoctorName, MedicalSpecialty,
         MedicalReportStatus, MedicalReportReceivedDate)
    VALUES
        (@CaseF3, 'Traumatic Brain Injury with Post-Concussion Syndrome', 'Head / Brain', 'Severe',
         1, 15.00,
         'Referred to Specialist', 'Gleneagles Hospital Kuala Lumpur', 'Dr. Ravindran a/l Nair', 'Neurology',
         'Received', '2023-09-15');

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF4)
    INSERT INTO InjuryRecord
        (CaseFileId, InjuryDescription, BodyPart, SeverityLevel,
         IsPermanentDisability, DisabilityPercentage,
         TreatmentStatus, HospitalName, DoctorName, MedicalSpecialty,
         MedicalReportStatus, MedicalReportReceivedDate)
    VALUES
        (@CaseF4, 'Lumbar Disc Herniation (L4-L5) with Nerve Compression', 'Lower Back / Lumbar Spine', 'Severe',
         1, 20.00,
         'Surgery Required', 'Sunway Medical Centre', 'Dr. Lim Chee Keong', 'Orthopaedic / Spinal',
         'Pending', NULL);

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF8)
    INSERT INTO InjuryRecord
        (CaseFileId, InjuryDescription, BodyPart, SeverityLevel,
         IsPermanentDisability, DisabilityPercentage,
         TreatmentStatus, HospitalName, DoctorName, MedicalSpecialty,
         MedicalReportStatus, MedicalReportReceivedDate)
    VALUES
        (@CaseF8, 'Laceration and Road Rash — Right Lower Leg', 'Right Leg', 'Minor',
         0, NULL,
         'Discharged', 'Klinik Kesihatan Petaling Jaya', 'Dr. Siti Hajar binti Mohd', 'General Practice',
         'Requested', NULL);

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF11)
    INSERT INTO InjuryRecord
        (CaseFileId, InjuryDescription, BodyPart, SeverityLevel,
         IsPermanentDisability, DisabilityPercentage,
         TreatmentStatus, HospitalName, DoctorName, MedicalSpecialty,
         MedicalReportStatus, MedicalReportReceivedDate)
    VALUES
        (@CaseF11, 'Fracture of Right Clavicle', 'Right Clavicle / Collarbone', 'Moderate',
         0, NULL,
         'Ongoing', 'Hospital Sultanah Aminah Johor Bahru', 'Dr. Mohamad Faizal bin Hamid', 'Orthopaedic',
         'Requested', NULL);

PRINT '   InjuryRecord: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 8. CASE DOCUMENTS
-- Model columns: CaseFileId, DocumentName, DocumentCategory, ExpectedFrom,
--                IsReceived, ReceivedDate, CollectionStatus,
--                DigitalStoragePath, OriginalFileName, FileMimeType,
--                UploadedBy, UploadedAt, Remarks
-- NOTE: No DocumentType column — renamed to DocumentName + DocumentCategory
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 8 ] Seeding CaseDocument...';

-- Case 1 documents
IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF1 AND DocumentName = 'Traffic Police Report')
    INSERT INTO CaseDocument
        (CaseFileId, DocumentName, DocumentCategory, ExpectedFrom,
         IsReceived, ReceivedDate, CollectionStatus,
         OriginalFileName, FileMimeType, UploadedBy, UploadedAt, Remarks)
    VALUES
        (@CaseF1, 'Traffic Police Report', 'Police', 'Police',
         1, '2024-01-20', 'Received',
         'policeReport_MVA20240001.pdf', 'application/pdf',
         'ahmad.farhan', '2024-01-21', 'Johor Bahru Traffic Police Report No. 00123/2024');

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF1 AND DocumentName = 'Orthopaedic Medical Report')
    INSERT INTO CaseDocument
        (CaseFileId, DocumentName, DocumentCategory, ExpectedFrom,
         IsReceived, ReceivedDate, CollectionStatus,
         OriginalFileName, FileMimeType, UploadedBy, UploadedAt, Remarks)
    VALUES
        (@CaseF1, 'Orthopaedic Medical Report', 'Medical', 'Hospital',
         1, '2024-02-10', 'Received',
         'medReport_MVA20240001.pdf', 'application/pdf',
         'ahmad.farhan', '2024-02-11', 'HKL Orthopaedic — Dr. Azman bin Yusof');

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF1 AND DocumentName = 'Insurance Policy Schedule')
    INSERT INTO CaseDocument
        (CaseFileId, DocumentName, DocumentCategory, ExpectedFrom,
         IsReceived, ReceivedDate, CollectionStatus,
         OriginalFileName, FileMimeType, UploadedBy, UploadedAt, Remarks)
    VALUES
        (@CaseF1, 'Insurance Policy Schedule', 'Insurance', 'Insurer',
         0, NULL, 'Requested',
         NULL, NULL,
         NULL, NULL, 'Requested from Allianz on 25/02/2024. Follow up due 10/03/2024.');

-- Case 3 documents (litigation)
IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF3 AND DocumentName = 'Writ of Summons')
    INSERT INTO CaseDocument
        (CaseFileId, DocumentName, DocumentCategory, ExpectedFrom,
         IsReceived, ReceivedDate, CollectionStatus,
         OriginalFileName, FileMimeType, UploadedBy, UploadedAt, Remarks)
    VALUES
        (@CaseF3, 'Writ of Summons', 'Legal', 'Court Registry',
         1, '2023-08-01', 'Received',
         'writ_MVA20230047.pdf', 'application/pdf',
         'ahmad.farhan', '2023-08-02', 'Mahkamah Tinggi KL Case No. MT-22NCVC-1234-2023');

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF3 AND DocumentName = 'Statement of Claim')
    INSERT INTO CaseDocument
        (CaseFileId, DocumentName, DocumentCategory, ExpectedFrom,
         IsReceived, ReceivedDate, CollectionStatus,
         OriginalFileName, FileMimeType, UploadedBy, UploadedAt, Remarks)
    VALUES
        (@CaseF3, 'Statement of Claim', 'Legal', 'Client',
         1, '2023-08-05', 'Received',
         'statementOfClaim_MVA20230047.pdf', 'application/pdf',
         'ahmad.farhan', '2023-08-05', 'Particulars of claim served on defendant via process server');

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF3 AND DocumentName = 'Neurological Medical Report')
    INSERT INTO CaseDocument
        (CaseFileId, DocumentName, DocumentCategory, ExpectedFrom,
         IsReceived, ReceivedDate, CollectionStatus,
         OriginalFileName, FileMimeType, UploadedBy, UploadedAt, Remarks)
    VALUES
        (@CaseF3, 'Neurological Medical Report', 'Medical', 'Hospital',
         1, '2023-09-15', 'Received',
         'neuroReport_MVA20230047.pdf', 'application/pdf',
         'ahmad.farhan', '2023-09-16', 'Dr. Ravindran — Gleneagles neurology specialist report');

-- Case 4 documents
IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF4 AND DocumentName = 'Spinal Specialist Report')
    INSERT INTO CaseDocument
        (CaseFileId, DocumentName, DocumentCategory, ExpectedFrom,
         IsReceived, ReceivedDate, CollectionStatus,
         OriginalFileName, FileMimeType, UploadedBy, UploadedAt, Remarks)
    VALUES
        (@CaseF4, 'Spinal Specialist Report', 'Medical', 'Hospital',
         0, NULL, 'Requested',
         NULL, NULL,
         NULL, NULL, 'Requested from Sunway Medical Centre — Dr. Lim. Due 30/07/2024. Follow up weekly.');

-- Case 2 documents
IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF2 AND DocumentName = 'Traffic Police Report')
    INSERT INTO CaseDocument
        (CaseFileId, DocumentName, DocumentCategory, ExpectedFrom,
         IsReceived, ReceivedDate, CollectionStatus,
         OriginalFileName, FileMimeType, UploadedBy, UploadedAt, Remarks)
    VALUES
        (@CaseF2, 'Traffic Police Report', 'Police', 'Police',
         1, '2023-11-25', 'Received',
         'policeReport_MVA20240002.pdf', 'application/pdf',
         'siti.norzahra', '2023-11-26', 'Petaling Jaya Traffic Police Report No. 04567/2023');

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF2 AND DocumentName = 'Orthopaedic Medical Report')
    INSERT INTO CaseDocument
        (CaseFileId, DocumentName, DocumentCategory, ExpectedFrom,
         IsReceived, ReceivedDate, CollectionStatus,
         OriginalFileName, FileMimeType, UploadedBy, UploadedAt, Remarks)
    VALUES
        (@CaseF2, 'Orthopaedic Medical Report', 'Medical', 'Hospital',
         1, '2024-01-05', 'Received',
         'medReport_MVA20240002.pdf', 'application/pdf',
         'siti.norzahra', '2024-01-06', 'Pantai Hospital — Dr. Selvarajah fracture specialist report');

PRINT '   CaseDocument: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 9. CASE DISBURSEMENTS
-- Model columns: CaseFileId, DisbursementDate, DisbursementCategory,
--                Description, Payee, Amount, PaymentMethod, PaymentReference,
--                ReceiptNumber, ReceiptStoragePath,
--                IsRecovered, RecoveredDate, RecordedBy, CreatedAt
-- NOTE: No ReceiptReference column — use ReceiptNumber instead
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 9 ] Seeding CaseDisbursement...';

-- Case 1 disbursements
IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF1 AND DisbursementCategory = 'Court Filing Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF1, @M2, 'Court Filing Fee',
         'Magistrate Court filing fee — Statement of Claim',
         'Mahkamah Sesyen Kuala Lumpur',
         350.00, 'Cheque', 'RCPT-2024-0112', 0, 'ahmad.farhan', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF1 AND DisbursementCategory = 'Medical Report Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF1, @M1, 'Medical Report Fee',
         'Orthopaedic medical report — Dr. Azman bin Yusof',
         'Hospital Kuala Lumpur',
         450.00, 'Online Transfer', 'RCPT-2024-0198', 0, 'ahmad.farhan', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF1 AND DisbursementCategory = 'Police Report Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF1, @M2, 'Police Report Fee',
         'Certified copy of traffic police report',
         'Johor Bahru Traffic Police',
         50.00, 'Cash', 'RCPT-2024-0087', 0, 'ahmad.farhan', GETDATE());

-- Case 3 disbursements (litigation — higher costs)
IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF3 AND DisbursementCategory = 'Court Filing Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF3, @M3, 'Court Filing Fee',
         'Writ of Summons filing fee — Mahkamah Tinggi Kuala Lumpur',
         'Mahkamah Tinggi Kuala Lumpur',
         800.00, 'Cheque', 'RCPT-2023-0445', 0, 'ahmad.farhan', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF3 AND DisbursementCategory = 'Expert Witness Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF3, @M1, 'Expert Witness Fee',
         'Accident reconstruction expert testimony — Dr. Ibrahim Khalid',
         'Dr. Ibrahim Khalid & Associates',
         2200.00, 'Cheque', 'RCPT-2024-0301', 0, 'ahmad.farhan', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF3 AND DisbursementCategory = 'Medical Report Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF3, @M2, 'Medical Report Fee',
         'Specialist neurological report — Gleneagles Hospital',
         'Gleneagles Hospital Kuala Lumpur',
         800.00, 'Online Transfer', 'RCPT-2024-0255', 0, 'ahmad.farhan', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF3 AND DisbursementCategory = 'Process Server Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF3, @M3, 'Process Server Fee',
         'Service of Writ of Summons on defendant',
         'Messrs Rapid Process Server',
         250.00, 'Cash', 'RCPT-2023-0512', 0, 'ahmad.farhan', GETDATE());

-- Case 4 disbursements
IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF4 AND DisbursementCategory = 'Court Filing Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF4, @M3, 'Court Filing Fee',
         'Writ of Summons filing fee — Mahkamah Tinggi Shah Alam',
         'Mahkamah Tinggi Shah Alam',
         800.00, 'Cheque', 'RCPT-2023-0600', 0, 'tan.boon.huat', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF4 AND DisbursementCategory = 'Medical Report Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF4, @M2, 'Medical Report Fee',
         'Spinal specialist report — Dr. Lim Chee Keong',
         'Sunway Medical Centre',
         1200.00, 'Online Transfer', 'RCPT-2024-0189', 0, 'tan.boon.huat', GETDATE());

-- Case 2 disbursements
IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF2 AND DisbursementCategory = 'Court Filing Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF2, @M2, 'Court Filing Fee',
         'Magistrate court filing fee',
         'Mahkamah Majistret Petaling Jaya',
         220.00, 'Cheque', 'RCPT-2024-0145', 0, 'siti.norzahra', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF2 AND DisbursementCategory = 'Medical Report Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF2, @M1, 'Medical Report Fee',
         'Fracture specialist report — Dr. Selvarajah',
         'Pantai Hospital Kuala Lumpur',
         350.00, 'Online Transfer', 'RCPT-2024-0267', 0, 'siti.norzahra', GETDATE());

-- Case 12 disbursements
IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF12 AND DisbursementCategory = 'Court Filing Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF12, @M3, 'Court Filing Fee',
         'High Court filing — complex personal injury claim',
         'Mahkamah Tinggi Johor Bahru',
         1200.00, 'Cheque', 'RCPT-2023-0701', 0, 'tan.boon.huat', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF12 AND DisbursementCategory = 'Expert Witness Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF12, @M1, 'Expert Witness Fee',
         'Orthopaedic specialist testimony — Prof. Dr. Selvam a/l Rajan',
         'Orthopaedic Specialists Malaysia',
         3000.00, 'Cheque', 'RCPT-2024-0388', 0, 'tan.boon.huat', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF12 AND DisbursementCategory = 'Interpreter Fee')
    INSERT INTO CaseDisbursement
        (CaseFileId, DisbursementDate, DisbursementCategory, Description, Payee,
         Amount, PaymentMethod, ReceiptNumber, IsRecovered, RecordedBy, CreatedAt)
    VALUES
        (@CaseF12, @M1, 'Interpreter Fee',
         'Tamil court interpreter — 2 hearing sessions',
         'Jabatan Kehakiman Sarawak Interpreter Unit',
         400.00, 'Cash', 'RCPT-2024-0412', 0, 'tan.boon.huat', GETDATE());

PRINT '   CaseDisbursement: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 10. CASE JOURNALS
-- Model columns: CaseFileId, AuthorCaseworkerId, EntryDateTime, EntryCategory,
--                EntryText, Visibility, IsFlagged, FollowUpAction, FollowUpDueDate
-- NOTE: No EntryType or Notes columns — use EntryCategory + EntryText
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 10 ] Seeding CaseJournal...';

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CaseJournal')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF1 AND EntryCategory = 'General')
        INSERT INTO CaseJournal
            (CaseFileId, AuthorCaseworkerId, EntryDateTime, EntryCategory,
             EntryText, Visibility, IsFlagged, FollowUpAction, FollowUpDueDate)
        VALUES
            (@CaseF1, @CW1, '2024-02-01 09:30:00', 'General',
             'New MVA case opened. Client Mohammad Hafiz attended office with original police report and hospital discharge summary. Accident on Jalan Duta on 15/01/2024 — rear-ended by lorry at traffic light. Client was driver. Liability appears clear. Allianz General Insurance identified as third-party insurer. Documents checklist issued to client.',
             'All Staff', 0, 'Obtain insurance policy schedule from Allianz', '2024-03-01');

    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF1 AND EntryCategory = 'Insurer Communication')
        INSERT INTO CaseJournal
            (CaseFileId, AuthorCaseworkerId, EntryDateTime, EntryCategory,
             EntryText, Visibility, IsFlagged, FollowUpAction, FollowUpDueDate)
        VALUES
            (@CaseF1, @CW1, '2024-02-20 14:15:00', 'Insurer Communication',
             'Letter of demand sent to Allianz General Insurance by AR Registered Post. Claiming MYR 85,000 comprising general damages (pain and suffering, loss of amenities) and special damages (medical expenses, transportation, loss of earnings). 14-day response period given. Copy filed.',
             'All Staff', 0, 'Follow up if no response by 06/03/2024', '2024-03-06');

    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF3 AND EntryCategory = 'Court Update')
        INSERT INTO CaseJournal
            (CaseFileId, AuthorCaseworkerId, EntryDateTime, EntryCategory,
             EntryText, Visibility, IsFlagged, FollowUpAction, FollowUpDueDate)
        VALUES
            (@CaseF3, @CW1, '2023-08-01 10:00:00', 'Court Update',
             'Writ of Summons and Statement of Claim filed at Mahkamah Tinggi Kuala Lumpur. Court assigned case number MT-22NCVC-1234-2023. Filing receipt obtained. Process server engaged to serve writ on defendant at registered address. Defendant has 14 days to enter appearance upon service.',
             'All Staff', 0, 'Confirm service of writ with process server', '2023-08-15');

    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF3 AND EntryCategory = 'Legal Strategy')
        INSERT INTO CaseJournal
            (CaseFileId, AuthorCaseworkerId, EntryDateTime, EntryCategory,
             EntryText, Visibility, IsFlagged, FollowUpAction, FollowUpDueDate)
        VALUES
            (@CaseF3, @CW1, '2023-08-22 11:30:00', 'Legal Strategy',
             'Defendant entered appearance through Messrs Chong & Partners. Statement of defence filed — defendant denies full liability, claims contributory negligence by plaintiff (20%). We will contest this. Expert witness Dr. Ibrahim engaged for accident reconstruction. Trial preparation commenced.',
             'Solicitors Only', 1, 'File Reply to Defence within 14 days', '2023-09-05');

    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF2 AND EntryCategory = 'Insurer Communication')
        INSERT INTO CaseJournal
            (CaseFileId, AuthorCaseworkerId, EntryDateTime, EntryCategory,
             EntryText, Visibility, IsFlagged, FollowUpAction, FollowUpDueDate)
        VALUES
            (@CaseF2, @CW2, '2024-02-15 15:00:00', 'Insurer Communication',
             'AXA Affin adjuster called — offered MYR 28,000 full and final settlement. Client instructed to reject as injuries are more serious than initially assessed. Counter-offer of MYR 35,000 sent by email and registered post. Awaiting insurer response within 7 days.',
             'All Staff', 1, 'Chase AXA for response to counter-offer', '2024-02-22');

    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF4 AND EntryCategory = 'Medical Update')
        INSERT INTO CaseJournal
            (CaseFileId, AuthorCaseworkerId, EntryDateTime, EntryCategory,
             EntryText, Visibility, IsFlagged, FollowUpAction, FollowUpDueDate)
        VALUES
            (@CaseF4, @CW6, '2023-05-30 09:00:00', 'Medical Update',
             'Client Nur Aisyah attended with updated medical notes from Sunway Medical Centre. Spinal injury confirmed as L4-L5 disc herniation requiring surgery. Surgeon recommends operation within 3 months. Disability assessment pending post-surgery. This materially increases the quantum of damages. Spinal specialist report requested urgently.',
             'All Staff', 1, 'Obtain spinal specialist report before trial', CAST(DATEADD(DAY, -5, GETDATE()) AS DATE));

    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF12 AND EntryCategory = 'Court Update')
        INSERT INTO CaseJournal
            (CaseFileId, AuthorCaseworkerId, EntryDateTime, EntryCategory,
             EntryText, Visibility, IsFlagged, FollowUpAction, FollowUpDueDate)
        VALUES
            (@CaseF12, @CW6, '2023-11-05 10:30:00', 'Court Update',
             'Case filed at Mahkamah Tinggi Johor Bahru. High-value claim MYR 145,000 — lorry collision on the North-South Expressway. Client Suresh sustained multiple orthopaedic injuries. Prof. Dr. Selvam engaged as expert witness. Tamil interpreter arranged for all court proceedings. Mention in 8 days.',
             'All Staff', 0, NULL, NULL);

    PRINT '   CaseJournal: OK';
END
ELSE
    PRINT '   CaseJournal: TABLE NOT FOUND — skipped';

-- ═══════════════════════════════════════════════════════════════════════════
-- SUMMARY
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '═══════════════════════════════════════════════════════════';
PRINT ' SECTIONS 6-10 COMPLETE — Record Counts:';
PRINT '═══════════════════════════════════════════════════════════';

SELECT 'HearingRecord'   AS [Table], COUNT(*) AS [Records] FROM HearingRecord   UNION ALL
SELECT 'InjuryRecord'    ,           COUNT(*)              FROM InjuryRecord     UNION ALL
SELECT 'CaseDocument'    ,           COUNT(*)              FROM CaseDocument     UNION ALL
SELECT 'CaseDisbursement',           COUNT(*)              FROM CaseDisbursement UNION ALL
SELECT 'CaseJournal'     ,           COUNT(*)              FROM CaseJournal
    WHERE EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CaseJournal');

PRINT '';
PRINT ' Completed: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '═══════════════════════════════════════════════════════════';
GO
