<?php
require_once 'includes/config.php';
require_auth();

$page_title = 'My Enrollments';

$enrollments = api_call('/enrollments/user/' . $_SESSION['user']['id'], 'GET', null, $_SESSION['token'] ?? null);

// Fallback demo data
if ($enrollments['code'] !== 200 || empty($enrollments['data'])) {
    $enrollments['data'] = [
        ['id' => 1, 'course' => 'Introduction to Programming', 'code' => 'CS101', 'status' => 'Active', 'enrolled_date' => '2026-08-15', 'grade' => '—'],
        ['id' => 2, 'course' => 'Database Systems', 'code' => 'CS201', 'status' => 'Completed', 'enrolled_date' => '2026-07-20', 'grade' => 'A'],
        ['id' => 3, 'course' => 'Web Development', 'code' => 'CS301', 'status' => 'Pending', 'enrolled_date' => '2026-08-18', 'grade' => '—'],
        ['id' => 4, 'course' => 'Data Structures', 'code' => 'CS102', 'status' => 'Completed', 'enrolled_date' => '2026-06-10', 'grade' => 'B+'],
    ];
}

if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['drop_id'])) {
    $drop_id = intval($_POST['drop_id']);
    $result = api_call('/enrollments/' . $drop_id, 'DELETE', null, $_SESSION['token'] ?? null);

    if ($result['code'] === 200 || $result['code'] === 204) {
        set_flash('success', 'Enrollment dropped successfully.');
    } else {
        set_flash('error', 'Failed to drop enrollment.');
    }
    header('Location: my-enrollments.php');
    exit;
}
?>
<?php include 'includes/header.php'; ?>

<div class="page-header">
    <h1>My Enrollments</h1>
    <p class="subtitle">Manage your course enrollments</p>
</div>

<div class="card">
    <table class="data-table">
        <thead>
            <tr>
                <th>Code</th>
                <th>Course</th>
                <th>Status</th>
                <th>Enrolled</th>
                <th>Grade</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            <?php foreach ($enrollments['data'] as $e): ?>
            <tr>
                <td><code><?php echo htmlspecialchars($e['code'] ?? 'N/A'); ?></code></td>
                <td><?php echo htmlspecialchars($e['course'] ?? 'N/A'); ?></td>
                <td><span class="badge badge-<?php echo strtolower($e['status'] ?? 'unknown'); ?>"><?php echo $e['status'] ?? 'N/A'; ?></span></td>
                <td><?php echo $e['enrolled_date'] ?? 'N/A'; ?></td>
                <td><?php echo $e['grade'] ?? '—'; ?></td>
                <td>
                    <?php if (($e['status'] ?? '') === 'Pending' || ($e['status'] ?? '') === 'Active'): ?>
                    <form method="POST" action="" style="display:inline;" onsubmit="return confirm('Drop this enrollment?');">
                        <input type="hidden" name="drop_id" value="<?php echo $e['id']; ?>">
                        <button type="submit" class="btn-danger-sm">Drop</button>
                    </form>
                    <?php else: ?>
                    <span class="text-muted">—</span>
                    <?php endif; ?>
                </td>
            </tr>
            <?php endforeach; ?>
        </tbody>
    </table>
</div>

<?php include 'includes/footer.php'; ?>
