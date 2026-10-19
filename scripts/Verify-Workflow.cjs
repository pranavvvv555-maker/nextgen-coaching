// Run only from Verify-FreshDatabase.ps1 against its disposable database.
const fs = require('fs'), path = require('path'), crypto = require('crypto');
const root = path.resolve(__dirname, '..');
const cfg = JSON.parse(fs.readFileSync(path.join(root, '.local/backend.json'), 'utf8').replace(/^\uFEFF/, ''));
const api = process.env.NEXTGEN_API_URL;
const checks = [];
function check(ok, name) { checks.push({ name, passed: !!ok }); if (!ok) throw Error(name); }
async function req(route, method = 'GET', body, token, status = 200) {
    const r = await fetch(api + route, { method, headers: { ...(body ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: 'Bearer ' + token } : {}) }, ...(body ? { body: JSON.stringify(body) } : {}) });
    const raw = await r.text(); let response; try { response = JSON.parse(raw); } catch { response = {}; }
    check(r.status === status, `${method} ${route} → ${status}${r.status === status ? '' : ': ' + (response.message || r.status)}`);
    return response.data;
}
async function login(role) { return (await req('/api/v1/auth/login', 'POST', { email: cfg.Seed[role + 'Email'], password: cfg.Seed[role + 'Password'] })).accessToken; }
async function run() {
    if (process.env.NEXTGEN_ISOLATED_WORKFLOW !== 'true' || api !== 'http://127.0.0.1:5111') throw Error('Use the disposable database verification script.');
    const admin = await login('Admin'), student = await login('Student'), instructor = await login('Instructor'), hr = await login('Hr');
    check((await req('/api/v1/admin/settings/email', 'GET', null, admin)).enabled === false, 'External email remains disabled');
    const sessions = await req('/api/v1/instructor/attendance/sessions', 'GET', null, instructor);
    check(sessions.length > 0, 'Fresh demo fixture includes instructor sessions');
    const sessionId = sessions[0].id, route = '/api/v1/instructor/attendance/sessions/' + sessionId;
    const roster = await req(route, 'GET', null, instructor);
    check(roster.rows.length > 0, 'Assigned instructor receives enrolled roster');
    const entry = { enrollmentId: roster.rows[0].enrollmentId, status: 'Late' };
    await req(route, 'POST', { entries: [entry, entry] }, instructor, 400);
    await req(route, 'POST', { entries: [{ ...entry, status: '999' }] }, instructor, 400);
    await req(route, 'POST', { entries: [{ ...entry, enrollmentId: crypto.randomUUID() }] }, instructor, 400);
    await req(route, 'POST', { entries: null }, instructor, 400);
    await req(route, 'POST', { entries: [null] }, instructor, 400);
    await req(route, 'POST', { entries: [entry] }, instructor);
    const attendance = await req('/api/v1/student/attendance', 'GET', null, student);
    check(attendance.late === 1 && attendance.totalSessions === 1, 'Attendance reaches the student portal');
    const batches = await req('/api/v1/admin/batches/', 'GET', null, admin);
    const batchId = batches.find(b => b.name === roster.batchName).id;
    const batch = await req('/api/v1/admin/batches/' + batchId, 'GET', null, admin);
    try {
        await req('/api/v1/admin/batches/' + batchId, 'PUT', { ...batch, primaryInstructorId: null }, admin);
        await req(route, 'GET', null, instructor, 403);
        await req(route, 'POST', { entries: [entry] }, instructor, 403);
        await req(route, 'GET', null, admin);
    } finally { await req('/api/v1/admin/batches/' + batchId, 'PUT', batch, admin); }
    const dashboard = await req('/api/v1/student/dashboard', 'GET', null, student);
    const assignment = dashboard.pendingAssignments.find(a => a.status === 'NotStarted');
    check(!!assignment, 'Student has an unsubmitted assignment');
    await req('/api/v1/student/assignments/' + assignment.assignmentId + '/submit', 'POST', {}, student, 400);
    const submitted = await req('/api/v1/student/assignments/' + assignment.assignmentId + '/submit', 'POST', { submissionText: 'Isolated QA submission.' }, student);
    await req('/api/v1/instructor/submissions/' + submitted.submissionId + '/grade', 'POST', { score: assignment.maxScore + 1 }, instructor, 400);
    await req('/api/v1/instructor/submissions/' + submitted.submissionId + '/grade', 'POST', { score: 80, feedback: 'Isolated QA feedback.' }, instructor);
    const graded = await req('/api/v1/student/assignments/' + assignment.assignmentId, 'GET', null, student);
    check(graded.score === 80 && graded.status === 'Graded', 'Instructor grade and feedback reach the student');
    await req('/api/v1/student/assignments/' + assignment.assignmentId + '/submit', 'POST', { submissionText: 'Attempt overwrite.' }, student, 400);
    const projects = await req('/api/v1/student/projects', 'GET', null, student);
    const project = projects.find(p => p.status === 'NotStarted');
    check(!!project, 'Student has a capstone project');
    const projectSubmit = await req('/api/v1/student/projects/' + project.id + '/submit', 'POST', { repoUrl: 'https://example.com/qa-repository', description: 'Isolated QA project.' }, student);
    await req('/api/v1/instructor/project-submissions/' + projectSubmit.submissionId + '/grade', 'POST', { totalScore: 101 }, instructor, 400);
    await req('/api/v1/instructor/project-submissions/' + projectSubmit.submissionId + '/grade', 'POST', { totalScore: 85, feedback: 'QA capstone feedback.' }, instructor);
    const projectResult = await req('/api/v1/student/projects/' + project.id, 'GET', null, student);
    check(projectResult.totalScore === 85 && projectResult.status === 'Graded', 'Capstone grading reaches the student');
    const job = { companyName: 'Isolated QA Company', roleTitle: 'Data Analyst', description: 'Disposable role.', skillsRequired: ['SQL'], minCtcLpa: 5, maxCtcLpa: 8, workMode: 'Remote', location: 'India' };
    await req('/api/v1/hr/jobs', 'POST', { ...job, minCtcLpa: 10, maxCtcLpa: 5 }, hr, 400);
    const jobId = await req('/api/v1/hr/jobs', 'POST', job, hr);
    await req('/api/v1/hr/jobs/' + jobId, 'PUT', { ...job, roleTitle: 'Junior Data Analyst' }, hr);
    await req('/api/v1/student/jobs/' + jobId + '/apply', 'POST', {}, student);
    await req('/api/v1/student/jobs/' + jobId + '/apply', 'POST', {}, student);
    const referrals = await req('/api/v1/hr/referrals', 'GET', null, hr);
    const applications = referrals.filter(r => r.jobOpeningId === jobId);
    check(applications.length === 1, 'Repeated application creates only one referral');
    await req('/api/v1/hr/referrals/' + applications[0].id + '/status', 'POST', { status: 'Interviewing', notes: 'QA status.' }, hr);
    await req('/api/v1/hr/jobs/' + jobId + '/close', 'POST', {}, hr);
    await req('/api/v1/student/jobs/' + jobId + '/apply', 'POST', {}, student, 404);
    const form = new FormData(); form.set('file', new Blob(['%PDF-1.7\n% QA fixture\n%%EOF'], { type: 'application/pdf' }), 'workflow.pdf');
    const upload = await fetch(api + '/api/v1/student/resumes', { method: 'POST', headers: { Authorization: 'Bearer ' + student }, body: form });
    check(upload.status === 200, 'Student resume uploads');
    const resumes = await req('/api/v1/student/resumes', 'GET', null, student);
    await req('/api/v1/hr/resumes/' + resumes[0].id + '/review', 'POST', { status: 'Approved', notes: 'QA approved.' }, hr);
    const reviewed = await req('/api/v1/student/resumes', 'GET', null, student);
    check(reviewed.find(r => r.id === resumes[0].id).status === 'Approved', 'Resume approval reaches student');
    const notifications = await req('/api/v1/student/notifications', 'GET', null, student);
    check(notifications.some(n => n.type === 'AssignmentGraded') && notifications.some(n => n.type === 'ProjectGraded') && notifications.some(n => n.type === 'ResumeApproved'), 'Workflow notifications created');
    await req('/api/v1/student/notifications/read-all', 'POST', {}, student);
    check((await req('/api/v1/student/notifications', 'GET', null, student)).every(n => n.isRead), 'Student can mark notifications read');
}
run().catch(e => { checks.push({ name: e.message, passed: false }); console.error(e.message); process.exitCode = 1; }).finally(() => {
    fs.writeFileSync(path.join(root, '.local/workflow-checks.json'), JSON.stringify(checks, null, 2));
    console.log(`${checks.filter(c => c.passed).length} workflow checks passed; ${checks.filter(c => !c.passed).length} failed.`);
});
