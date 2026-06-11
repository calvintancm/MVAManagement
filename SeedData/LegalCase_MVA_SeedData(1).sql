-- ═══════════════════════════════════════════════════════════════════════════
-- LegalCase MVA — Sample Data Seed Script
-- Database: SQL Server (Compatibility Level 120)
-- Purpose : Populate all master + transaction tables with realistic
--           Malaysian MVA legal case sample data
-- Run     : Execute in SSMS against your MVAManagement database
-- Safe    : All inserts use IF NOT EXISTS guards — safe to re-run
-- ═══════════════════════════════════════════════════════════════════════════


GO

SET NOCOUNT ON;

-- ── Collect all FK IDs upfront ────────────────────────────────────────────
DECLARE @VenueKL    INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MTKL');
DECLARE @VenueSHA   INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MTSHA');
DECLARE @VenueJB    INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MSJB');
DECLARE @VenueKLSes INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MSKL');


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

PRINT '═══════════════════════════════════════════════════════════';
PRINT ' LegalCase MVA — Seed Data Script';
PRINT ' Started: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '═══════════════════════════════════════════════════════════';

-- ═══════════════════════════════════════════════════════════════════════════
-- 1. CASE STATUSES
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 1 ] Seeding CaseStatus...';

IF NOT EXISTS (SELECT 1 FROM CaseStatus WHERE StatusCode = 'ACT')
    INSERT INTO CaseStatus (StatusName, StatusCode, DisplayOrder)
    VALUES ('Active', 'ACT', 1);

IF NOT EXISTS (SELECT 1 FROM CaseStatus WHERE StatusCode = 'NEG')
    INSERT INTO CaseStatus (StatusName, StatusCode, DisplayOrder)
    VALUES ('Under Negotiation', 'NEG', 2);

IF NOT EXISTS (SELECT 1 FROM CaseStatus WHERE StatusCode = 'LIT')
    INSERT INTO CaseStatus (StatusName, StatusCode, DisplayOrder)
    VALUES ('In Litigation', 'LIT', 3);

IF NOT EXISTS (SELECT 1 FROM CaseStatus WHERE StatusCode = 'SET')
    INSERT INTO CaseStatus (StatusName, StatusCode, DisplayOrder)
    VALUES ('Settled', 'SET', 4);

IF NOT EXISTS (SELECT 1 FROM CaseStatus WHERE StatusCode = 'CLO')
    INSERT INTO CaseStatus (StatusName, StatusCode, DisplayOrder)
    VALUES ('Closed', 'CLO', 5);

IF NOT EXISTS (SELECT 1 FROM CaseStatus WHERE StatusCode = 'WIT')
    INSERT INTO CaseStatus (StatusName, StatusCode, DisplayOrder)
    VALUES ('Withdrawn', 'WIT', 6);

IF NOT EXISTS (SELECT 1 FROM CaseStatus WHERE StatusCode = 'HLD')
    INSERT INTO CaseStatus (StatusName, StatusCode, DisplayOrder)
    VALUES ('On Hold', 'HLD', 7);

PRINT '   CaseStatus: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 2. COURT VENUES
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 2 ] Seeding CourtVenue...';

IF NOT EXISTS (SELECT 1 FROM CourtVenue WHERE VenueCode = 'MTKL')
    INSERT INTO CourtVenue (VenueName, VenueCode, Jurisdiction, Address, ContactNumber)
    VALUES ('Mahkamah Tinggi Kuala Lumpur', 'MTKL',
            'Kuala Lumpur',
            'Jalan Tuanku Abdul Halim, 50480 Kuala Lumpur',
            '+603-2693 5400');

IF NOT EXISTS (SELECT 1 FROM CourtVenue WHERE VenueCode = 'MSKL')
    INSERT INTO CourtVenue (VenueName, VenueCode, Jurisdiction, Address, ContactNumber)
    VALUES ('Mahkamah Sesyen Kuala Lumpur', 'MSKL',
            'Kuala Lumpur',
            'Jalan Duta, 50480 Kuala Lumpur',
            '+603-2693 5500');

IF NOT EXISTS (SELECT 1 FROM CourtVenue WHERE VenueCode = 'MMKL')
    INSERT INTO CourtVenue (VenueName, VenueCode, Jurisdiction, Address, ContactNumber)
    VALUES ('Mahkamah Majistret Kuala Lumpur', 'MMKL',
            'Kuala Lumpur',
            'Jalan Duta, 50480 Kuala Lumpur',
            '+603-2693 5600');

IF NOT EXISTS (SELECT 1 FROM CourtVenue WHERE VenueCode = 'MTSHA')
    INSERT INTO CourtVenue (VenueName, VenueCode, Jurisdiction, Address, ContactNumber)
    VALUES ('Mahkamah Tinggi Shah Alam', 'MTSHA',
            'Shah Alam',
            'Persiaran Damai, Seksyen 17, 40200 Shah Alam, Selangor',
            '+603-5510 5000');

IF NOT EXISTS (SELECT 1 FROM CourtVenue WHERE VenueCode = 'MSJB')
    INSERT INTO CourtVenue (VenueName, VenueCode, Jurisdiction, Address, ContactNumber)
    VALUES ('Mahkamah Sesyen Johor Bahru', 'MSJB',
            'Johor Bahru',
            'Jalan Ayer Molek, 80000 Johor Bahru, Johor',
            '+607-224 4000');

IF NOT EXISTS (SELECT 1 FROM CourtVenue WHERE VenueCode = 'MTJB')
    INSERT INTO CourtVenue (VenueName, VenueCode, Jurisdiction, Address, ContactNumber)
    VALUES ('Mahkamah Tinggi Johor Bahru', 'MTJB',
            'Johor Bahru',
            'Jalan Bukit Timbalan, 80000 Johor Bahru, Johor',
            '+607-224 5000');

IF NOT EXISTS (SELECT 1 FROM CourtVenue WHERE VenueCode = 'MTPG')
    INSERT INTO CourtVenue (VenueName, VenueCode, Jurisdiction, Address, ContactNumber)
    VALUES ('Mahkamah Tinggi Pulau Pinang', 'MTPG',
            'George Town',
            'Lebuh Light, 10050 Georgetown, Pulau Pinang',
            '+604-261 4000');

IF NOT EXISTS (SELECT 1 FROM CourtVenue WHERE VenueCode = 'COA')
    INSERT INTO CourtVenue (VenueName, VenueCode, Jurisdiction, Address, ContactNumber)
    VALUES ('Court of Appeal Malaysia', 'COA',
            'Putrajaya',
            'Kompleks Mahkamah Putrajaya, 62506 Putrajaya',
            '+603-8880 3500');

IF NOT EXISTS (SELECT 1 FROM CourtVenue WHERE VenueCode = 'FC')
    INSERT INTO CourtVenue (VenueName, VenueCode, Jurisdiction, Address, ContactNumber)
    VALUES ('Federal Court Malaysia', 'FC',
            'Putrajaya',
            'Kompleks Mahkamah Putrajaya, 62506 Putrajaya',
            '+603-8880 3600');

PRINT '   CourtVenue: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 3. INSURER REGISTRY
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 3 ] Seeding InsurerRegistry...';

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'ALZ')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Allianz General Insurance Malaysia Berhad', 'ALZ', '+603-2264 0688', 'claims.motor@allianz.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'AXA')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('AXA Affin General Insurance Berhad', 'AXA', '+603-2170 8282', 'motor.claims@axa.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'ZUR')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Zurich General Insurance Malaysia Berhad', 'ZUR', '+603-2109 6000', 'motorclaims@zurich.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'MSIG')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('MSIG Insurance (Malaysia) Berhad', 'MSIG', '+603-2285 6000', 'claims@msig.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'ETIQA')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Etiqa General Insurance Berhad', 'ETIQA', '+603-5613 3333', 'motorclaim@etiqa.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'LONPAC')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Lonpac Insurance Bhd', 'LONPAC', '+603-2262 8688', 'motor@lonpac.com', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'TAKAFUL')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Takaful Malaysia General Berhad', 'TAKAFUL', '+603-2694 0500', 'claims@takaful.com.my', 1);

PRINT '   InsurerRegistry: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 4. CASEWORKER PROFILES
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 4 ] Seeding CaseworkerProfile...';

IF NOT EXISTS (SELECT 1 FROM CaseworkerProfile WHERE Username = 'ahmad.farhan')
    INSERT INTO CaseworkerProfile
        (IdentityUserId, FullName, Username, JobRole, SystemRole,
         Email, ContactNumber, JoinedDate, IsActive)
    VALUES (NULL, 'Ahmad Farhan bin Abdullah', 'ahmad.farhan',
            'Senior Solicitor', 'Solicitor',
            'ahmad.farhan@legalcasemva.com.my', '+6012-388 9900',
            '2018-03-01', 1);

IF NOT EXISTS (SELECT 1 FROM CaseworkerProfile WHERE Username = 'siti.norzahra')
    INSERT INTO CaseworkerProfile
        (IdentityUserId, FullName, Username, JobRole, SystemRole,
         Email, ContactNumber, JoinedDate, IsActive)
    VALUES (NULL, 'Siti Norzahra binti Yusoff', 'siti.norzahra',
            'Solicitor', 'Solicitor',
            'siti.norzahra@legalcasemva.com.my', '+6016-772 4411',
            '2020-07-15', 1);

IF NOT EXISTS (SELECT 1 FROM CaseworkerProfile WHERE Username = 'rajendran.k')
    INSERT INTO CaseworkerProfile
        (IdentityUserId, FullName, Username, JobRole, SystemRole,
         Email, ContactNumber, JoinedDate, IsActive)
    VALUES (NULL, 'Rajendran Krishnamurthy', 'rajendran.k',
            'Legal Executive', 'Caseworker',
            'rajendran.k@legalcasemva.com.my', '+6011-2234 5567',
            '2019-11-01', 1);

IF NOT EXISTS (SELECT 1 FROM CaseworkerProfile WHERE Username = 'lim.swee.ling')
    INSERT INTO CaseworkerProfile
        (IdentityUserId, FullName, Username, JobRole, SystemRole,
         Email, ContactNumber, JoinedDate, IsActive)
    VALUES (NULL, 'Lim Swee Ling', 'lim.swee.ling',
            'Paralegal', 'Caseworker',
            'lim.sweeling@legalcasemva.com.my', '+6019-554 3321',
            '2021-02-01', 1);

IF NOT EXISTS (SELECT 1 FROM CaseworkerProfile WHERE Username = 'nurul.huda')
    INSERT INTO CaseworkerProfile
        (IdentityUserId, FullName, Username, JobRole, SystemRole,
         Email, ContactNumber, JoinedDate, IsActive)
    VALUES (NULL, 'Nurul Huda binti Ismail', 'nurul.huda',
            'Legal Clerk', 'Caseworker',
            'nurul.huda@legalcasemva.com.my', '+6013-887 6654',
            '2022-06-01', 1);

IF NOT EXISTS (SELECT 1 FROM CaseworkerProfile WHERE Username = 'tan.boon.huat')
    INSERT INTO CaseworkerProfile
        (IdentityUserId, FullName, Username, JobRole, SystemRole,
         Email, ContactNumber, JoinedDate, IsActive)
    VALUES (NULL, 'Tan Boon Huat', 'tan.boon.huat',
            'Senior Solicitor', 'Solicitor',
            'tan.boonhuat@legalcasemva.com.my', '+6012-991 0033',
            '2017-05-10', 1);

PRINT '   CaseworkerProfile: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 5. CASE FILES
-- NOTE: StatuteOfLimitationsDeadline, IsLimitationExpired, ClaimantAge
--       are C# computed properties — NOT database columns. Do NOT insert them.
--       They are calculated at runtime from AccidentDate / DateOfBirth.
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 5 ] Seeding CaseFile...';

DECLARE @StatusACT  INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'ACT');
DECLARE @StatusNEG  INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'NEG');
DECLARE @StatusLIT  INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'LIT');
DECLARE @StatusSET  INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'SET');
DECLARE @StatusCLO  INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'CLO');
DECLARE @StatusHLD  INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'HLD');

DECLARE @CW1 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'ahmad.farhan');
DECLARE @CW2 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'siti.norzahra');
DECLARE @CW3 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'rajendran.k');
DECLARE @CW4 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'lim.swee.ling');
DECLARE @CW5 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'nurul.huda');
DECLARE @CW6 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'tan.boon.huat');

-- ── Case 1: Active — recent accident ─────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0001')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, TelephoneNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2024-0001', 'Mohammad Hafiz bin Kamaruddin',
     '1985-06-12', '850612-01-5234',
     '+6012-334 5567', '+603-7955 1122', 'Male',
     'No 14, Jalan Mawar 3, Taman Bunga, 47810 Petaling Jaya, Selangor',
     'Walk-in',
     '2024-01-15', 'Driver',
     @StatusACT, @CW1,
     1, 0, 0,
     85000.00, 60000.00, 0.00, 1250.00,
     NULL, NULL,
     'Client struck from behind at traffic light on Jalan Duta. Third-party insured with Allianz.',
     '2024-02-01', '2024-02-01');

-- ── Case 2: Under Negotiation ─────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0002')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2024-0002', 'Priya Devi a/p Subramaniam',
     '1990-03-28', '900328-10-6712',
     '+6016-778 9921', 'Female',
     'No 5, Jalan SS 2/64, 47300 Petaling Jaya, Selangor',
     'Referral - Dr. Rajan',
     '2023-11-20', 'Passenger',
     @StatusNEG, @CW2,
     1, 0, 0,
     42000.00, 30000.00, 28000.00, 870.00,
     NULL, NULL,
     'AXA offered MYR 28,000. Counter-offer of MYR 35,000 sent. Awaiting response.',
     '2024-01-10', '2024-03-05');

-- ── Case 3: In Litigation — hearing in 12 days ────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2023-0047')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, CourtCaseNumber, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, SummonsDraftedDate, SummonsSealedDate,
     Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2023-0047', 'Chong Wei Loon',
     'MT-22NCVC-1234-2023',
     '1978-11-05', '781105-14-3345',
     '+6012-655 8834', 'Male',
     'No 88, Jalan Klang Lama, 58000 Kuala Lumpur',
     'Agent - Tan KH',
     '2023-06-08', 'Driver',
     @StatusLIT, @CW1,
     1, 1, 0,
     120000.00, 85000.00, 75000.00, 4500.00,
     DATEADD(DAY, 12, GETDATE()), NULL,
     '2023-07-01', '2023-07-20',
     'Head injury. Full trial set. Expert witness booked. Defendant insured with Zurich.',
     '2023-07-15', '2024-02-20');

-- ── Case 4: In Litigation — URGENT hearing in 2 days ─────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2023-0051')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, CourtCaseNumber, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, SummonsDraftedDate, SummonsSealedDate,
     Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2023-0051', 'Nur Aisyah binti Razali',
     'BA-22NCVC-0567-2023',
     '1992-07-19', '920719-04-7823',
     '+6016-223 4456', 'Female',
     'No 3, Jalan Subang 5, USJ 1, 47600 Subang Jaya, Selangor',
     'Walk-in',
     '2023-04-22', 'Pillion Rider',
     @StatusLIT, @CW6,
     1, 1, 0,
     95000.00, 68000.00, 60000.00, 3200.00,
     DATEADD(DAY, 2, GETDATE()), NULL,
     '2023-05-10', '2023-05-28',
     'Day 1 of full trial in 2 days. Spinal injury. 3 witnesses subpoenaed.',
     '2023-05-30', '2024-03-01');

-- ── Case 5: Active — limitation expiring in ~60 days ─────────────────────
-- AccidentDate set so that AccidentDate + 6 years = ~60 days from now
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2018-0012')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2018-0012', 'Kelvin Ong Chee Keong',
     '1980-04-15', '800415-08-9901',
     '+6019-332 1177', 'Male',
     'No 22, Jalan Ipoh, 51200 Kuala Lumpur',
     'Referral - Klinik Dr. Lee',
     DATEADD(YEAR, -6, DATEADD(DAY, 60, GETDATE())), 'Driver',
     @StatusACT, @CW3,
     1, 0, 0,
     55000.00, 38000.00, 0.00, 500.00,
     NULL, NULL,
     'WARNING: Limitation expires in approximately 60 days. Urgently review.',
     '2018-03-01', '2024-01-10');

-- ── Case 6: Active — limitation expiring in ~15 days ─────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2018-0019')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2018-0019', 'Faridah binti Othman',
     '1975-09-30', '750930-03-4412',
     '+6013-556 7789', 'Female',
     'No 7, Lorong Matahari 2, Taman Desa, 58100 Kuala Lumpur',
     'Walk-in',
     DATEADD(YEAR, -6, DATEADD(DAY, 15, GETDATE())), 'Pedestrian',
     @StatusACT, @CW4,
     1, 0, 0,
     38000.00, 25000.00, 0.00, 200.00,
     NULL, NULL,
     'CRITICAL: Limitation expires in approximately 15 days. File writ immediately.',
     '2018-04-01', '2024-04-15');

-- ── Case 7: Active — limitation EXPIRED (30 days ago) ────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2017-0033')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2017-0033', 'Ramesh a/l Pillai',
     '1968-12-22', '681222-07-8834',
     '+6011-2345 6789', 'Male',
     'No 15, Jalan Maharajalela, 50150 Kuala Lumpur',
     'Agent - Muthu',
     DATEADD(YEAR, -6, DATEADD(DAY, -30, GETDATE())), 'Cyclist',
     @StatusACT, @CW5,
     1, 0, 0,
     67000.00, 45000.00, 0.00, 750.00,
     NULL, NULL,
     'EXPIRED: Limitation period has passed 30 days ago. Advise client immediately.',
     '2017-08-10', '2024-02-01');

-- ── Case 8: Under Negotiation ─────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0003')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2024-0003', 'Lee Cheng Yong',
     '1988-02-14', '880214-05-2291',
     '+6017-443 5566', 'Male',
     'No 33, Jalan Bandar Utama 3, 47800 Petaling Jaya, Selangor',
     'Referral - Tan Boon Huat',
     '2024-03-10', 'Driver',
     @StatusNEG, @CW2,
     1, 0, 0,
     28000.00, 20000.00, 18500.00, 620.00,
     NULL, NULL,
     'MSIG offered MYR 18,500. Counter-offer pending. Minor injury — laceration.',
     '2024-04-01', '2024-04-01');

-- ── Case 9: Settled ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2023-0022')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2023-0022', 'Zainab binti Hassan',
     '1982-05-07', '820507-06-3345',
     '+6012-778 4433', 'Female',
     'No 9, Jalan Ampang, 50450 Kuala Lumpur',
     'Walk-in',
     '2023-02-14', 'Driver',
     @StatusSET, @CW6,
     0, 0, 0,
     73000.00, 50000.00, 55000.00, 2100.00,
     NULL, '2024-05-20',
     'Settled at MYR 55,000. Full and final settlement signed. Cheque received.',
     '2023-03-01', '2024-05-20');

-- ── Case 10: Closed ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2022-0088')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2022-0088', 'Tan Ah Kow',
     '1970-08-20', '700820-10-1122',
     '+6019-221 3344', 'Male',
     'No 55, Jalan Chan Sow Lin, 55200 Kuala Lumpur',
     'Walk-in',
     '2022-09-05', 'Passenger',
     @StatusCLO, @CW1,
     0, 0, 1,
     15000.00, 10000.00, 12000.00, 450.00,
     NULL, '2024-01-15',
     'Closed. Settled at MYR 12,000 — minor whiplash. File archived.',
     '2022-10-01', '2024-01-15');

-- ── Case 11: Active — recent ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0004')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2024-0004', 'Hasnah binti Mohd Noor',
     '1995-11-03', '951103-07-4456',
     '+6011-3344 5566', 'Female',
     'No 12, Jalan Cemerlang 4, Taman Maju, 81300 Skudai, Johor',
     'Referral - Dr. Azman',
     '2024-05-01', 'Pillion Rider',
     @StatusACT, @CW3,
     1, 0, 0,
     62000.00, 45000.00, 0.00, 980.00,
     NULL, NULL,
     'Motorcycle accident. Pillion rider sustained fractured collarbone. Insurer: Etiqa.',
     '2024-05-20', '2024-05-20');

-- ── Case 12: In Litigation — hearing in 8 days ────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2023-0078')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, CourtCaseNumber, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, SummonsDraftedDate, SummonsSealedDate,
     Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2023-0078', 'Suresh a/l Nadarajan',
     'JB-22NCVC-0891-2023',
     '1973-04-18', '730418-01-6634',
     '+6012-889 0011', 'Male',
     'No 7, Jalan Stulang Darat, 80300 Johor Bahru, Johor',
     'Agent - Kumar',
     '2023-10-18', 'Driver',
     @StatusLIT, @CW6,
     1, 1, 0,
     145000.00, 100000.00, 90000.00, 5600.00,
     DATEADD(DAY, 8, GETDATE()), NULL,
     '2023-11-10', '2023-11-28',
     'High-value claim. Lorry collision. Orthopaedic specialist witness arranged.',
     '2023-11-05', '2024-04-01');

-- ── Case 13: On Hold ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0005')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2024-0005', 'Wong Fook Meng',
     '1983-07-25', '830725-14-7712',
     '+6016-554 8890', 'Male',
     'No 44, Jalan Bukit Bintang, 55100 Kuala Lumpur',
     'Walk-in',
     '2024-02-28', 'Driver',
     @StatusHLD, @CW4,
     1, 0, 0,
     49000.00, 35000.00, 0.00, 300.00,
     NULL, NULL,
     'On hold — client unable to be contacted. Medical report outstanding from Pantai Hospital.',
     '2024-03-15', '2024-03-15');

-- ── Case 14: Active — new ─────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0006')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2024-0006', 'Amirul Hakimi bin Zulkifli',
     '1997-01-30', '970130-03-5521',
     '+6013-667 4432', 'Male',
     'No 18, Jalan Sri Hartamas 1, 50480 Kuala Lumpur',
     'Referral - Messrs Lim & Co',
     '2024-04-12', 'Driver',
     @StatusACT, @CW5,
     1, 0, 0,
     33000.00, 22000.00, 0.00, 150.00,
     NULL, NULL,
     'Young driver. Rear-ended at Federal Highway. Insurer: Lonpac.',
     '2024-04-30', '2024-04-30');

-- ── Case 15: Settled — older ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2022-0045')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, DateOfBirth, NationalId,
     MobileNumber, Gender, FullAddress, ReferralSource,
     AccidentDate, ClientVehicleRole,
     CaseStatusId, AssignedCaseworkerId,
     IsActive, IsInLitigation, IsClosed,
     ClaimedAmount, MinimumSettlementTarget, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, Remarks, CreatedAt, UpdatedAt)
VALUES
    ('MVA-2022-0045', 'Kavitha a/p Govindasamy',
     '1979-03-12', '790312-10-4456',
     '+6011-7788 9900', 'Female',
     'No 6, Jalan Ampang Hilir, 55000 Kuala Lumpur',
     'Agent - Raj Kumar',
     '2022-05-18', 'Pedestrian',
     @StatusSET, @CW2,
     0, 0, 0,
     88000.00, 62000.00, 65000.00, 3800.00,
     NULL, '2023-11-30',
     'Settled at MYR 65,000. Pedestrian knocked down at zebra crossing. Takaful insurer.',
     '2022-06-01', '2023-11-30');

PRINT '   CaseFile: OK (15 cases seeded)';

-- ═══════════════════════════════════════════════════════════════════════════
-- 6. HEARING RECORDS
-- ═══════════════════════════════════════════════════════════════════════════
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
