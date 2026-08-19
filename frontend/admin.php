<?php
require_once 'includes/config.php';
require_admin();

$page_title = 'Admin Panel';

$users = api_call('/users', 'GET', null, $_SESSION['token'] ?? null);
$all_enrollments = api_call('/enrollments', 'GET', null, $_SESSION['token'] ?? null);

// Fallback demo data
if ($users['code'] !== 200 || empty($users['data'])) {
    $users['data'] = [
        ['id' => 1, 'name' => 'Demo User', 'email' => 'demo@nexus.com', 'role' => 'Student'],
        ['id' => 2, 'name' => 'Jane Smith', 'email' => 'jane@example.com', 'role' => 'Student'],
        ['id' => 3, 'name' => 'Prof. Johnson', 'email' => 'johnson@example.com', 'role' => 'Instructor'],
    ];
}
if ($all_enrollments['code'] !== 200 || empty($all_enrollments['data'])) {
    $all_enrollments['data'] = [
        ['id' => 1, 'student' => 'Demo User', 'course' => 'CS101', 'status' => 'Active', 'date' => '2026-08-15'],
        ['id' => 2, 'student' => 'Jane Smith', 'course' => 'CS201', 'status' => 'Completed', 'date' => '2026-07-20'],
    ];
}
?>
<?php include 'includes/header.php'; ?>

<div class="page-header">
    <h1>Admin Panel</h1>
    <p class="subtitle">System overview and management</p>
</div>

<div class="admin-sections">
    <div class="card">
        <div class="card-header"><h2>All Users</h2></div>
        <table class="data-table">
            <thead>
                <tr><th>ID</th><th>Name</th><th>Email</th><th>Role</th></tr>
            </thead>
            <tbody>
                <?php foreach ($users['data'] as $u): ?>
                <tr>
                    <td><?php echo $u['id']; ?></td>
                    <td><?php echo htmlspecialchars($u['name'] ?? 'N/A'); ?></td>
                    <td><?php echo htmlspecialchars($u['email'] ?? 'N/A'); ?></td>
                    <td><span class="badge badge-<?php echo strtolower($u['role'] ?? 'student'); ?>"><?php echo $u['role'] ?? 'N/A'; ?></span></td>
                </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    </div>

    <div class="card">
        <div class="card-header"><h2>All Enrollments</h2></div>
        <table class="data-table">
            <thead>
                <tr><th>ID</th><th>Student</th><th>Course</th><th>Status</th><th>Date</th></tr>
            </thead>
            <tbody>
                <?php foreach ($all_enrollments['data'] as $e): ?>
                <tr>
                    <td><?php echo $e['id']; ?></td>
                    <td><?php echo htmlspecialchars($e['student'] ?? 'N/A'); ?></td>
                    <td><?php echo htmlspecialchars($e['course'] ?? 'N/A'); ?></td>
                    <td><span class="badge badge-<?php echo strtolower($e['status'] ?? 'unknown'); ?>"><?php echo $e['status'] ?? 'N/A'; ?></span></td>
                    <td><?php echo $e['date'] ?? 'N/A'; ?></td>
                </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    </div>
</div>

<?php include 'includes/footer.php'; ?>
