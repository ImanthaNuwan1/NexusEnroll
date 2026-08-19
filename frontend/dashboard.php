<?php
require_once 'includes/config.php';
require_auth();

$page_title = 'Dashboard';

// Fetch data from C# backend
$stats = api_call('/dashboard/stats', 'GET', null, $_SESSION['token'] ?? null);
$recent = api_call('/enrollments/recent', 'GET', null, $_SESSION['token'] ?? null);

// Fallback demo data if backend not ready
if ($stats['code'] !== 200 || empty($stats['data'])) {
    $stats['data'] = [
        'total_enrollments' => 12,
        'active_courses' => 4,
        'completed' => 8,
        'pending' => 2
    ];
}
if ($recent['code'] !== 200 || empty($recent['data'])) {
    $recent['data'] = [
        ['course' => 'Introduction to Programming', 'status' => 'Active', 'date' => '2026-08-15'],
        ['course' => 'Database Systems', 'status' => 'Completed', 'date' => '2026-07-20'],
        ['course' => 'Web Development', 'status' => 'Pending', 'date' => '2026-08-18'],
    ];
}
?>
<?php include 'includes/header.php'; ?>

<div class="page-header">
    <h1>Dashboard</h1>
    <p class="subtitle">Welcome back, <?php echo htmlspecialchars($_SESSION['user']['name']); ?></p>
</div>

<div class="stats-grid">
    <div class="stat-card">
        <div class="stat-icon">📚</div>
        <div class="stat-value"><?php echo $stats['data']['total_enrollments'] ?? 0; ?></div>
        <div class="stat-label">Total Enrollments</div>
    </div>
    <div class="stat-card">
        <div class="stat-icon">🎓</div>
        <div class="stat-value"><?php echo $stats['data']['active_courses'] ?? 0; ?></div>
        <div class="stat-label">Active Courses</div>
    </div>
    <div class="stat-card">
        <div class="stat-icon">✅</div>
        <div class="stat-value"><?php echo $stats['data']['completed'] ?? 0; ?></div>
        <div class="stat-label">Completed</div>
    </div>
    <div class="stat-card">
        <div class="stat-icon">⏳</div>
        <div class="stat-value"><?php echo $stats['data']['pending'] ?? 0; ?></div>
        <div class="stat-label">Pending</div>
    </div>
</div>

<div class="card">
    <div class="card-header">
        <h2>Recent Enrollments</h2>
        <a href="my-enrollments.php" class="btn-link">View All →</a>
    </div>
    <table class="data-table">
        <thead>
            <tr>
                <th>Course</th>
                <th>Status</th>
                <th>Enrolled On</th>
                <th>Action</th>
            </tr>
        </thead>
        <tbody>
            <?php foreach ($recent['data'] as $item): ?>
            <tr>
                <td><?php echo htmlspecialchars($item['course'] ?? 'N/A'); ?></td>
                <td><span class="badge badge-<?php echo strtolower($item['status'] ?? 'unknown'); ?>"><?php echo $item['status'] ?? 'N/A'; ?></span></td>
                <td><?php echo $item['date'] ?? 'N/A'; ?></td>
                <td><a href="enroll.php" class="btn-sm">Details</a></td>
            </tr>
            <?php endforeach; ?>
        </tbody>
    </table>
</div>

<div class="quick-actions">
    <a href="enroll.php" class="action-card">
        <div class="action-icon">➕</div>
        <h3>New Enrollment</h3>
        <p>Enroll in a new course or program</p>
    </a>
    <a href="profile.php" class="action-card">
        <div class="action-icon">👤</div>
        <h3>My Profile</h3>
        <p>Update your personal information</p>
    </a>
    <a href="my-enrollments.php" class="action-card">
        <div class="action-icon">📋</div>
        <h3>History</h3>
        <p>View all your enrollment records</p>
    </a>
</div>

<?php include 'includes/footer.php'; ?>
