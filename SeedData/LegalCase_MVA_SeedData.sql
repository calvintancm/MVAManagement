-- ═══════════════════════════════════════════════════════════════════════════
-- LegalCase MVA — Sample Data Seed Script
-- Database: SQL Server (Compatibility Level 120)
-- Purpose : Populate all master + transaction tables with realistic
--           Malaysian MVA legal case sample data
-- Run     : Execute in SSMS against your MVAManagement database
-- Safe    : All inserts use IF NOT EXISTS guards — safe to re-run
-- ═══════════════════════════════════════════════════════════════════════════

USE MVAManagement;
GO

SET NOCOUNT ON;
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

PRINT '   CaseStatus: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows affected';

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

IF NOT EXISTS (SELECT 1 FROM CourtVenue WHERE VenueCode = 'MTIPOH')
    INSERT INTO CourtVenue (VenueName, VenueCode, Jurisdiction, Address, ContactNumber)
    VALUES ('Mahkamah Tinggi Ipoh', 'MTIPOH',
            'Ipoh',
            'Jalan Dato Tahwil Azar, 30000 Ipoh, Perak',
            '+605-253 9000');

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
    VALUES ('Allianz General Insurance Malaysia Berhad', 'ALZ',
            '+603-2264 0688', 'claims.motor@allianz.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'AXA')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('AXA Affin General Insurance Berhad', 'AXA',
            '+603-2170 8282', 'motor.claims@axa.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'PRU')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Prudential BSN Takaful Berhad', 'PRU',
            '+603-2053 7188', 'claims@prudential.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'ZUR')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Zurich General Insurance Malaysia Berhad', 'ZUR',
            '+603-2109 6000', 'motorclaims@zurich.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'MSIG')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('MSIG Insurance (Malaysia) Berhad', 'MSIG',
            '+603-2285 6000', 'claims@msig.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'BUMI')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Berjaya Sompo Insurance Berhad', 'BUMI',
            '+603-2141 6060', 'claims.motor@berjayasompo.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'PROG')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Progressive Insurance Bhd', 'PROG',
            '+603-7627 3888', 'motorclaims@progressive.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'TAKAFUL')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Takaful Malaysia General Berhad', 'TAKAFUL',
            '+603-2694 0500', 'claims@takaful.com.my', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'LONPAC')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Lonpac Insurance Bhd', 'LONPAC',
            '+603-2262 8688', 'motor@lonpac.com', 1);

IF NOT EXISTS (SELECT 1 FROM InsurerRegistry WHERE InsurerCode = 'ETIQA')
    INSERT INTO InsurerRegistry (InsurerName, InsurerCode, ClaimsContactNumber, ClaimsEmail, IsActive)
    VALUES ('Etiqa General Insurance Berhad', 'ETIQA',
            '+603-5613 3333', 'motorclaim@etiqa.com.my', 1);

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
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 5 ] Seeding CaseFile...';

-- Collect foreign key IDs
DECLARE @StatusACT   INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'ACT');
DECLARE @StatusNEG   INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'NEG');
DECLARE @StatusLIT   INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'LIT');
DECLARE @StatusSET   INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'SET');
DECLARE @StatusCLO   INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'CLO');
DECLARE @StatusHLD   INT = (SELECT Id FROM CaseStatus WHERE StatusCode = 'HLD');

DECLARE @CW1 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'ahmad.farhan');
DECLARE @CW2 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'siti.norzahra');
DECLARE @CW3 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'rajendran.k');
DECLARE @CW4 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'lim.swee.ling');
DECLARE @CW5 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'nurul.huda');
DECLARE @CW6 INT = (SELECT Id FROM CaseworkerProfile WHERE Username = 'tan.boon.huat');

-- Case 1: Active — recent accident
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0001')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2024-0001', 'Mohammad Hafiz bin Kamaruddin',
     '2024-01-15', '2030-01-15',
     @StatusACT, @CW1, 1, 0,
     85000.00, 0.00, 1250.00,
     NULL, NULL, '2024-02-01');

-- Case 2: Under Negotiation
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0002')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2024-0002', 'Priya Devi a/p Subramaniam',
     '2023-11-20', '2029-11-20',
     @StatusNEG, @CW2, 1, 0,
     42000.00, 28000.00, 870.00,
     NULL, NULL, '2024-01-10');

-- Case 3: In Litigation — upcoming hearing
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2023-0047')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2023-0047', 'Chong Wei Loon',
     '2023-06-08', '2029-06-08',
     @StatusLIT, @CW1, 1, 1,
     120000.00, 75000.00, 4500.00,
     DATEADD(DAY, 12, GETDATE()), NULL, '2023-07-15');

-- Case 4: In Litigation — hearing in 3 days (urgent)
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2023-0051')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2023-0051', 'Nur Aisyah binti Razali',
     '2023-04-22', '2029-04-22',
     @StatusLIT, @CW6, 1, 1,
     95000.00, 60000.00, 3200.00,
     DATEADD(DAY, 2, GETDATE()), NULL, '2023-05-30');

-- Case 5: Active — limitation alert (expires in 60 days)
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2018-0012')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2018-0012', 'Kelvin Ong Chee Keong',
     '2018-01-01', DATEADD(DAY, 60, GETDATE()),
     @StatusACT, @CW3, 1, 0,
     55000.00, 0.00, 500.00,
     NULL, NULL, '2018-03-01');

-- Case 6: Active — limitation almost expired (15 days)
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2018-0019')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2018-0019', 'Faridah binti Othman',
     '2018-01-15', DATEADD(DAY, 15, GETDATE()),
     @StatusACT, @CW4, 1, 0,
     38000.00, 0.00, 200.00,
     NULL, NULL, '2018-04-01');

-- Case 7: Active — limitation EXPIRED
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2017-0033')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2017-0033', 'Ramesh a/l Pillai',
     '2017-06-01', DATEADD(DAY, -30, GETDATE()),
     @StatusACT, @CW5, 1, 0,
     67000.00, 0.00, 750.00,
     NULL, NULL, '2017-08-10');

-- Case 8: Negotiation
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0003')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2024-0003', 'Lee Cheng Yong',
     '2024-03-10', '2030-03-10',
     @StatusNEG, @CW2, 1, 0,
     28000.00, 18500.00, 620.00,
     NULL, NULL, '2024-04-01');

-- Case 9: Settled
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2023-0022')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2023-0022', 'Zainab binti Hassan',
     '2023-02-14', '2029-02-14',
     @StatusSET, @CW6, 0, 0,
     73000.00, 55000.00, 2100.00,
     NULL, '2024-05-20', '2023-03-01');

-- Case 10: Closed
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2022-0088')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2022-0088', 'Tan Ah Kow',
     '2022-09-05', '2028-09-05',
     @StatusCLO, @CW1, 0, 0,
     15000.00, 12000.00, 450.00,
     NULL, '2024-01-15', '2022-10-01');

-- Case 11: Active — ongoing
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0004')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2024-0004', 'Hasnah binti Mohd Noor',
     '2024-05-01', '2030-05-01',
     @StatusACT, @CW3, 1, 0,
     62000.00, 0.00, 980.00,
     NULL, NULL, '2024-05-20');

-- Case 12: Litigation — hearing in 8 days
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2023-0078')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2023-0078', 'Suresh a/l Nadarajan',
     '2023-10-18', '2029-10-18',
     @StatusLIT, @CW6, 1, 1,
     145000.00, 90000.00, 5600.00,
     DATEADD(DAY, 8, GETDATE()), NULL, '2023-11-05');

-- Case 13: On Hold
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0005')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2024-0005', 'Wong Fook Meng',
     '2024-02-28', '2030-02-28',
     @StatusHLD, @CW4, 1, 0,
     49000.00, 0.00, 300.00,
     NULL, NULL, '2024-03-15');

-- Case 14: Active
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2024-0006')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2024-0006', 'Amirul Hakimi bin Zulkifli',
     '2024-04-12', '2030-04-12',
     @StatusACT, @CW5, 1, 0,
     33000.00, 0.00, 150.00,
     NULL, NULL, '2024-04-30');

-- Case 15: Settled — older
IF NOT EXISTS (SELECT 1 FROM CaseFile WHERE FileNumber = 'MVA-2022-0045')
INSERT INTO CaseFile
    (FileNumber, PrimaryClaimantName, AccidentDate, StatuteOfLimitationsDeadline,
     CaseStatusId, AssignedCaseworkerId, IsActive, IsInLitigation,
     ClaimedAmount, CurrentOffer, TotalDisbursementAmount,
     NextHearingDate, CaseClosedDate, CreatedAt)
VALUES
    ('MVA-2022-0045', 'Kavitha a/p Govindasamy',
     '2022-05-18', '2028-05-18',
     @StatusSET, @CW2, 0, 0,
     88000.00, 65000.00, 3800.00,
     NULL, '2023-11-30', '2022-06-01');

PRINT '   CaseFile: OK (15 cases seeded)';

-- ═══════════════════════════════════════════════════════════════════════════
-- 6. HEARING RECORDS
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 6 ] Seeding HearingRecord...';

DECLARE @VenueKL   INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MTKL');
DECLARE @VenueSHA  INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MTSHA');
DECLARE @VenueJB   INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MSJB');
DECLARE @VenueKLSes INT = (SELECT Id FROM CourtVenue WHERE VenueCode = 'MSKL');

DECLARE @Case3  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2023-0047');
DECLARE @Case4  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2023-0051');
DECLARE @Case12 INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2023-0078');
DECLARE @Case1  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2024-0001');

-- Upcoming hearings for litigation cases
IF NOT EXISTS (SELECT 1 FROM HearingRecord WHERE CaseFileId = @Case3 AND ScheduledDate = CAST(DATEADD(DAY,12,GETDATE()) AS DATE))
INSERT INTO HearingRecord
    (CaseFileId, CourtVenueId, ScheduledDate, HearingType,
     IsCompleted, Notes, CreatedAt)
VALUES
    (@Case3, @VenueKL, DATEADD(DAY,12,GETDATE()),
     'Case Management', 0,
     'Pre-trial case management conference. Both parties to file witness statements.',
     GETDATE());

IF NOT EXISTS (SELECT 1 FROM HearingRecord WHERE CaseFileId = @Case4 AND ScheduledDate = CAST(DATEADD(DAY,2,GETDATE()) AS DATE))
INSERT INTO HearingRecord
    (CaseFileId, CourtVenueId, ScheduledDate, HearingType,
     IsCompleted, Notes, CreatedAt)
VALUES
    (@Case4, @VenueSHA, DATEADD(DAY,2,GETDATE()),
     'Full Trial', 0,
     'Day 1 of full trial. Plaintiff to call 3 witnesses. Medical expert booked.',
     GETDATE());

IF NOT EXISTS (SELECT 1 FROM HearingRecord WHERE CaseFileId = @Case12 AND ScheduledDate = CAST(DATEADD(DAY,8,GETDATE()) AS DATE))
INSERT INTO HearingRecord
    (CaseFileId, CourtVenueId, ScheduledDate, HearingType,
     IsCompleted, Notes, CreatedAt)
VALUES
    (@Case12, @VenueJB, DATEADD(DAY,8,GETDATE()),
     'Mention', 0,
     'Mention for case management directions.',
     GETDATE());

-- Past completed hearing for Case 3
IF NOT EXISTS (SELECT 1 FROM HearingRecord WHERE CaseFileId = @Case3 AND IsCompleted = 1)
INSERT INTO HearingRecord
    (CaseFileId, CourtVenueId, ScheduledDate, HearingType,
     IsCompleted, Notes, CreatedAt)
VALUES
    (@Case3, @VenueKL, DATEADD(MONTH,-2,GETDATE()),
     'Mention', 1,
     'First mention. Defendant filed statement of defence. Next hearing set.',
     DATEADD(MONTH,-3,GETDATE()));

-- Upcoming hearing for active case
IF NOT EXISTS (SELECT 1 FROM HearingRecord WHERE CaseFileId = @Case1 AND IsCompleted = 0)
INSERT INTO HearingRecord
    (CaseFileId, CourtVenueId, ScheduledDate, HearingType,
     IsCompleted, Notes, CreatedAt)
VALUES
    (@Case1, @VenueKLSes, DATEADD(DAY,20,GETDATE()),
     'Mediation', 0,
     'Court-directed mediation session.',
     GETDATE());

PRINT '   HearingRecord: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 7. INJURY RECORDS
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 7 ] Seeding InjuryRecord...';

DECLARE @CaseF1  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2024-0001');
DECLARE @CaseF2  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2024-0002');
DECLARE @CaseF3  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2023-0047');
DECLARE @CaseF4  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2023-0051');
DECLARE @CaseF8  INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2024-0003');

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF1)
INSERT INTO InjuryRecord
    (CaseFileId, InjuryType, BodyPart, Severity,
     HospitalName, TreatmentDate,
     MedicalReportStatus, CreatedAt)
VALUES
    (@CaseF1, 'Soft Tissue Injury', 'Neck / Cervical Spine', 'Moderate',
     'Hospital Kuala Lumpur', '2024-01-16',
     'Received', GETDATE());

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF2)
INSERT INTO InjuryRecord
    (CaseFileId, InjuryType, BodyPart, Severity,
     HospitalName, TreatmentDate,
     MedicalReportStatus, CreatedAt)
VALUES
    (@CaseF2, 'Fracture', 'Left Arm / Radius', 'Serious',
     'Pantai Hospital Kuala Lumpur', '2023-11-21',
     'Received', GETDATE());

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF3)
INSERT INTO InjuryRecord
    (CaseFileId, InjuryType, BodyPart, Severity,
     HospitalName, TreatmentDate,
     MedicalReportStatus, CreatedAt)
VALUES
    (@CaseF3, 'Head Injury / Concussion', 'Head', 'Serious',
     'Gleneagles Hospital Kuala Lumpur', '2023-06-09',
     'Received', GETDATE());

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF4)
INSERT INTO InjuryRecord
    (CaseFileId, InjuryType, BodyPart, Severity,
     HospitalName, TreatmentDate,
     MedicalReportStatus, CreatedAt)
VALUES
    (@CaseF4, 'Spinal Injury', 'Lower Back / Lumbar', 'Severe',
     'Sunway Medical Centre', '2023-04-23',
     'Pending', GETDATE());

IF NOT EXISTS (SELECT 1 FROM InjuryRecord WHERE CaseFileId = @CaseF8)
INSERT INTO InjuryRecord
    (CaseFileId, InjuryType, BodyPart, Severity,
     HospitalName, TreatmentDate,
     MedicalReportStatus, CreatedAt)
VALUES
    (@CaseF8, 'Laceration / Abrasion', 'Right Leg', 'Minor',
     'Klinik Kesihatan Petaling Jaya', '2024-03-10',
     'Requested', GETDATE());

PRINT '   InjuryRecord: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 8. CASE DOCUMENTS
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 8 ] Seeding CaseDocument...';

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF1 AND DocumentType = 'Police Report')
INSERT INTO CaseDocument
    (CaseFileId, DocumentType, FileName, CollectionStatus, IsReceived, ReceivedDate, Notes, CreatedAt)
VALUES
    (@CaseF1, 'Police Report', 'policeReport_MVA20240001.pdf',
     'Received', 1, '2024-01-20',
     'Johor Bahru Traffic Police Report No. 00123/2024', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF1 AND DocumentType = 'Medical Report')
INSERT INTO CaseDocument
    (CaseFileId, DocumentType, FileName, CollectionStatus, IsReceived, ReceivedDate, Notes, CreatedAt)
VALUES
    (@CaseF1, 'Medical Report', 'medReport_MVA20240001.pdf',
     'Received', 1, '2024-02-10',
     'HKL Orthopaedic — Dr. Azman bin Yusof', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF1 AND DocumentType = 'Insurance Policy')
INSERT INTO CaseDocument
    (CaseFileId, DocumentType, FileName, CollectionStatus, IsReceived, ReceivedDate, Notes, CreatedAt)
VALUES
    (@CaseF1, 'Insurance Policy', NULL,
     'Pending', 0, NULL,
     'Awaiting from Allianz — requested 25/02/2024', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF3 AND DocumentType = 'Writ of Summons')
INSERT INTO CaseDocument
    (CaseFileId, DocumentType, FileName, CollectionStatus, IsReceived, ReceivedDate, Notes, CreatedAt)
VALUES
    (@CaseF3, 'Writ of Summons', 'writ_MVA20230047.pdf',
     'Received', 1, '2023-08-01',
     'Mahkamah Tinggi KL Case No. MT-22NCVC-1234-2023', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF3 AND DocumentType = 'Statement of Claim')
INSERT INTO CaseDocument
    (CaseFileId, DocumentType, FileName, CollectionStatus, IsReceived, ReceivedDate, Notes, CreatedAt)
VALUES
    (@CaseF3, 'Statement of Claim', 'statementOfClaim_MVA20230047.pdf',
     'Received', 1, '2023-08-05',
     'Particulars of claim served on defendant', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF4 AND DocumentType = 'Medical Report')
INSERT INTO CaseDocument
    (CaseFileId, DocumentType, FileName, CollectionStatus, IsReceived, ReceivedDate, Notes, CreatedAt)
VALUES
    (@CaseF4, 'Medical Report', NULL,
     'Requested', 0, NULL,
     'Spinal specialist report from Sunway — due 30/07/2024', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDocument WHERE CaseFileId = @CaseF2 AND DocumentType = 'Police Report')
INSERT INTO CaseDocument
    (CaseFileId, DocumentType, FileName, CollectionStatus, IsReceived, ReceivedDate, Notes, CreatedAt)
VALUES
    (@CaseF2, 'Police Report', 'policeReport_MVA20240002.pdf',
     'Received', 1, '2023-11-25',
     'Petaling Jaya Traffic Police Report', GETDATE());

PRINT '   CaseDocument: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 9. CASE DISBURSEMENTS
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 9 ] Seeding CaseDisbursement...';

DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @M1    DATE = CAST(DATEADD(MONTH,-1,GETDATE()) AS DATE);
DECLARE @M2    DATE = CAST(DATEADD(MONTH,-2,GETDATE()) AS DATE);
DECLARE @M3    DATE = CAST(DATEADD(MONTH,-3,GETDATE()) AS DATE);

-- Case 1 disbursements
IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF1 AND DisbursementCategory = 'Filing Fees')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF1, 'Filing Fees', 350.00, @M2, 'Court filing fee — Statement of Claim', 'RCPT-2024-0112', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF1 AND DisbursementCategory = 'Medical Report Fee')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF1, 'Medical Report Fee', 450.00, @M1, 'HKL orthopaedic medical report fee', 'RCPT-2024-0198', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF1 AND DisbursementCategory = 'Police Report Fee')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF1, 'Police Report Fee', 50.00, @M2, 'Certified copy of police report', 'RCPT-2024-0087', GETDATE());

-- Case 3 disbursements (litigation — more fees)
IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF3 AND DisbursementCategory = 'Filing Fees')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF3, 'Filing Fees', 800.00, @M3, 'Writ of Summons filing fee — Mahkamah Tinggi', 'RCPT-2023-0445', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF3 AND DisbursementCategory = 'Expert Witness Fee')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF3, 'Expert Witness Fee', 2200.00, @M1, 'Accident reconstruction expert — Dr. Ibrahim', 'RCPT-2024-0301', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF3 AND DisbursementCategory = 'Medical Report Fee')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF3, 'Medical Report Fee', 800.00, @M2, 'Specialist neurological report — Gleneagles', 'RCPT-2024-0255', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF3 AND DisbursementCategory = 'Process Server Fee')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF3, 'Process Server Fee', 250.00, @M3, 'Service of writ on defendant', 'RCPT-2023-0512', GETDATE());

-- Case 4 disbursements
IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF4 AND DisbursementCategory = 'Filing Fees')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF4, 'Filing Fees', 800.00, @M3, 'Writ filing — Shah Alam High Court', 'RCPT-2023-0600', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF4 AND DisbursementCategory = 'Medical Report Fee')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF4, 'Medical Report Fee', 1200.00, @M2, 'Spinal specialist report — Sunway Medical', 'RCPT-2024-0189', GETDATE());

-- Case 2 disbursements
IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF2 AND DisbursementCategory = 'Filing Fees')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF2, 'Filing Fees', 220.00, @M2, 'Magistrate court filing fee', 'RCPT-2024-0145', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF2 AND DisbursementCategory = 'Medical Report Fee')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF2, 'Medical Report Fee', 350.00, @M1, 'Pantai Hospital fracture specialist report', 'RCPT-2024-0267', GETDATE());

-- Case 12 disbursements
DECLARE @CaseF12 INT = (SELECT Id FROM CaseFile WHERE FileNumber = 'MVA-2023-0078');

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF12 AND DisbursementCategory = 'Filing Fees')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF12, 'Filing Fees', 1200.00, @M3, 'High Court filing — complex injury claim', 'RCPT-2023-0701', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF12 AND DisbursementCategory = 'Expert Witness Fee')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF12, 'Expert Witness Fee', 3000.00, @M1, 'Orthopaedic specialist testimony fee', 'RCPT-2024-0388', GETDATE());

IF NOT EXISTS (SELECT 1 FROM CaseDisbursement WHERE CaseFileId = @CaseF12 AND DisbursementCategory = 'Interpreter Fee')
INSERT INTO CaseDisbursement (CaseFileId, DisbursementCategory, Amount, DisbursementDate, Description, ReceiptReference, CreatedAt)
VALUES (@CaseF12, 'Interpreter Fee', 400.00, @M1, 'Tamil interpreter — 2 sessions', 'RCPT-2024-0412', GETDATE());

PRINT '   CaseDisbursement: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- 10. CASE JOURNALS
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 10 ] Seeding CaseJournal...';

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CaseJournal')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF1 AND EntryType = 'Case Opened')
    INSERT INTO CaseJournal (CaseFileId, CaseworkerProfileId, EntryType, Notes, CreatedAt)
    VALUES (@CaseF1, @CW1, 'Case Opened',
            'New MVA case opened. Client attended office with police report and medical documents. Accident occurred at Jalan Duta. Opposing vehicle was a lorry. Liability assessment ongoing.',
            '2024-02-01');

    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF1 AND EntryType = 'Letter of Demand')
    INSERT INTO CaseJournal (CaseFileId, CaseworkerProfileId, EntryType, Notes, CreatedAt)
    VALUES (@CaseF1, @CW1, 'Letter of Demand',
            'Letter of demand sent to Allianz General Insurance. Claimed MYR 85,000 for general and special damages. 14-day response period.',
            '2024-02-20');

    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF3 AND EntryType = 'Writ Filed')
    INSERT INTO CaseJournal (CaseFileId, CaseworkerProfileId, EntryType, Notes, CreatedAt)
    VALUES (@CaseF3, @CW1, 'Writ Filed',
            'Writ of Summons and Statement of Claim filed at Mahkamah Tinggi Kuala Lumpur. Case management conference scheduled. Defendant has 14 days to enter appearance.',
            '2023-08-01');

    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF3 AND EntryType = 'Defendant Appearance')
    INSERT INTO CaseJournal (CaseFileId, CaseworkerProfileId, EntryType, Notes, CreatedAt)
    VALUES (@CaseF3, @CW1, 'Defendant Appearance',
            'Defendant entered appearance through Messrs Chong & Partners. Statement of defence to be filed within 14 days.',
            '2023-08-22');

    IF NOT EXISTS (SELECT 1 FROM CaseJournal WHERE CaseFileId = @CaseF2 AND EntryType = 'Offer Received')
    INSERT INTO CaseJournal (CaseFileId, CaseworkerProfileId, EntryType, Notes, CreatedAt)
    VALUES (@CaseF2, @CW2, 'Offer Received',
            'AXA Affin offered MYR 28,000 in full and final settlement. Client considering. Counter-offer of MYR 35,000 prepared for discussion.',
            '2024-02-15');

    PRINT '   CaseJournal: OK';
END
ELSE
    PRINT '   CaseJournal: TABLE NOT FOUND — skipped';

-- ═══════════════════════════════════════════════════════════════════════════
-- 11. DISBURSEMENT CATEGORIES (lookup table — if exists)
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 11 ] Seeding DisbursementCategory...';

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DisbursementCategory')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM DisbursementCategory WHERE CategoryName = 'Filing Fees')
        INSERT INTO DisbursementCategory (CategoryName, Description, HexColor, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
        VALUES ('Filing Fees', 'Court filing and registration fees', '#185FA5', 1, 1, GETDATE(), GETDATE());

    IF NOT EXISTS (SELECT 1 FROM DisbursementCategory WHERE CategoryName = 'Medical Report Fee')
        INSERT INTO DisbursementCategory (CategoryName, Description, HexColor, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
        VALUES ('Medical Report Fee', 'Specialist and hospital medical report fees', '#16A34A', 2, 1, GETDATE(), GETDATE());

    IF NOT EXISTS (SELECT 1 FROM DisbursementCategory WHERE CategoryName = 'Expert Witness Fee')
        INSERT INTO DisbursementCategory (CategoryName, Description, HexColor, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
        VALUES ('Expert Witness Fee', 'Accident reconstruction, medical expert testimony fees', '#F59E0B', 3, 1, GETDATE(), GETDATE());

    IF NOT EXISTS (SELECT 1 FROM DisbursementCategory WHERE CategoryName = 'Police Report Fee')
        INSERT INTO DisbursementCategory (CategoryName, Description, HexColor, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
        VALUES ('Police Report Fee', 'Certified police report copies', '#DC2626', 4, 1, GETDATE(), GETDATE());

    IF NOT EXISTS (SELECT 1 FROM DisbursementCategory WHERE CategoryName = 'Process Server Fee')
        INSERT INTO DisbursementCategory (CategoryName, Description, HexColor, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
        VALUES ('Process Server Fee', 'Service of legal documents on defendants', '#9333EA', 5, 1, GETDATE(), GETDATE());

    IF NOT EXISTS (SELECT 1 FROM DisbursementCategory WHERE CategoryName = 'Interpreter Fee')
        INSERT INTO DisbursementCategory (CategoryName, Description, HexColor, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
        VALUES ('Interpreter Fee', 'Court interpreter and translation services', '#0891B2', 6, 1, GETDATE(), GETDATE());

    IF NOT EXISTS (SELECT 1 FROM DisbursementCategory WHERE CategoryName = 'Stamp Duty')
        INSERT INTO DisbursementCategory (CategoryName, Description, HexColor, DisplayOrder, IsActive, CreatedAt, UpdatedAt)
        VALUES ('Stamp Duty', 'LHDN stamp duty on legal documents', '#6B7280', 7, 1, GETDATE(), GETDATE());

    PRINT '   DisbursementCategory: OK';
END
ELSE
    PRINT '   DisbursementCategory: TABLE NOT FOUND — run migration first';

-- ═══════════════════════════════════════════════════════════════════════════
-- 12. AUDIT SESSION LOG SAMPLE
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '[ 12 ] Seeding AuditSessionLog (sample sessions)...';

IF NOT EXISTS (SELECT 1 FROM AuditSessionLog WHERE SessionId = 'SEED-SESSION-001')
INSERT INTO AuditSessionLog
    (UserId, Username, SessionId, IpAddress, ComputerName, UserAgent,
     LoginTime, LogoutTime, IsActive, LogoutReason, IsFlaggedSuspicious)
VALUES
    ('seed-user-id-001', 'admin',
     'SEED-SESSION-001', '192.168.1.100', 'WORKSTATION-01',
     'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0',
     DATEADD(HOUR,-3,GETDATE()), DATEADD(HOUR,-2,GETDATE()),
     0, 'User Logout', 0);

IF NOT EXISTS (SELECT 1 FROM AuditSessionLog WHERE SessionId = 'SEED-SESSION-002')
INSERT INTO AuditSessionLog
    (UserId, Username, SessionId, IpAddress, ComputerName, UserAgent,
     LoginTime, LogoutTime, IsActive, LogoutReason, IsFlaggedSuspicious)
VALUES
    ('seed-user-id-002', 'ahmad.farhan',
     'SEED-SESSION-002', '192.168.1.105', 'WORKSTATION-02',
     'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Edge/120.0',
     DATEADD(HOUR,-1,GETDATE()), NULL,
     1, NULL, 0);

IF NOT EXISTS (SELECT 1 FROM AuditSessionLog WHERE SessionId = 'SEED-SESSION-003')
INSERT INTO AuditSessionLog
    (UserId, Username, SessionId, IpAddress, ComputerName, UserAgent,
     LoginTime, LogoutTime, IsActive, LogoutReason, IsFlaggedSuspicious, SuspicionReason)
VALUES
    ('seed-user-id-003', 'unknown.user',
     'SEED-SESSION-003', '103.28.54.22', NULL,
     'Python-urllib/3.9',
     DATEADD(DAY,-1,GETDATE()), DATEADD(DAY,-1,GETDATE()),
     0, 'Forced Termination', 1,
     'Login from overseas IP (103.28.54.22). Outside business hours. Non-browser user agent.');

PRINT '   AuditSessionLog: OK';

-- ═══════════════════════════════════════════════════════════════════════════
-- SUMMARY
-- ═══════════════════════════════════════════════════════════════════════════
PRINT '';
PRINT '═══════════════════════════════════════════════════════════';
PRINT ' SEED COMPLETE — Record Counts:';
PRINT '═══════════════════════════════════════════════════════════';

SELECT 'CaseStatus'         AS TableName, COUNT(*) AS Records FROM CaseStatus          UNION ALL
SELECT 'CourtVenue'         ,             COUNT(*)            FROM CourtVenue           UNION ALL
SELECT 'InsurerRegistry'    ,             COUNT(*)            FROM InsurerRegistry      UNION ALL
SELECT 'CaseworkerProfile'  ,             COUNT(*)            FROM CaseworkerProfile    UNION ALL
SELECT 'CaseFile'           ,             COUNT(*)            FROM CaseFile             UNION ALL
SELECT 'HearingRecord'      ,             COUNT(*)            FROM HearingRecord        UNION ALL
SELECT 'InjuryRecord'       ,             COUNT(*)            FROM InjuryRecord         UNION ALL
SELECT 'CaseDocument'       ,             COUNT(*)            FROM CaseDocument         UNION ALL
SELECT 'CaseDisbursement'   ,             COUNT(*)            FROM CaseDisbursement     UNION ALL
SELECT 'AuditSessionLog'    ,             COUNT(*)            FROM AuditSessionLog;

PRINT '';
PRINT ' Completed: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '═══════════════════════════════════════════════════════════';
GO
