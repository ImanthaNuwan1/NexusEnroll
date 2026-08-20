<?php
require_once 'includes/config.php';
require_auth();

$page_title = 'Enroll';

// Fetch available courses from C# backend
$courses = api_call('/courses/available', 'GET', null, $_SESSION['token'] ?? null);

// Fallback demo data
if ($courses['code'] !== 200 || empty($courses['data'])) {
    $courses['data'] = [
        ['id' => 1, 'title' => 'Introduction to Programming', 'code' => 'CS101', 'credits' => 3, 'instructor' => 'Dr. Smith', 'slots' => 15],
        ['id' => 2, 'title' => 'Database Systems', 'code' => 'CS201', 'credits' => 4, 'instructor' => 'Prof. Johnson', 'slots' => 8],
        ['id' => 3, 'title' => 'Web Development', 'code' => 'CS301', 'credits' => 3, 'instructor' => 'Ms. Davis', 'slots' => 20],
        ['id' => 4, 'title' => 'Data Structures', 'code' => 'CS102', 'credits' => 3, 'instructor' => 'Dr. Brown', 'slots' => 5],
        ['id' => 5, 'title' => 'Software Engineering', 'code' => 'CS401', 'credits' => 4, 'instructor' => 'Prof. Wilson', 'slots' => 12],
    ];
}

if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['course_id'])) {
    $course_id = trim($_POST['course_id']);

    $result = api_call('/enrollments', 'POST', [
        'userId' => $_SESSION['user']['id'],
        'courseId' => $course_id
    ], $_SESSION['token'] ?? null);

    if ($result['code'] === 201 || $result['code'] === 200) {
        set_flash('success', 'Successfully enrolled!');
    } else {
        set_flash('error', $result['data']['message'] ?? 'Enrollment failed. Please try again.');
    }
    header('Location: enroll.php');
    exit;
}
?>
<?php include 'includes/header.php'; ?>

<div class="page-header">
    <h1>Course Enrollment</h1>
    <p class="subtitle">Browse and enroll in available courses</p>
</div>

<div class="course-grid">
    <?php foreach ($courses['data'] as $course): ?>
    <div class="course-card">
        <div class="course-header">
            <span class="course-code"><?php echo htmlspecialchars($course['code'] ?? 'N/A'); ?></span>
            <span class="course-credits"><?php echo $course['credits'] ?? 0; ?> Credits</span>
        </div>
        <h3 class="course-title"><?php echo htmlspecialchars($course['title'] ?? 'Untitled'); ?></h3>
        <p class="course-instructor">👤 <?php echo htmlspecialchars($course['instructor'] ?? 'TBA'); ?></p>
        <div class="course-meta">
            <span class="slots"><?php echo $course['slots'] ?? 0; ?> slots left</span>
        </div>
        <form method="POST" action="">
            <input type="hidden" name="course_id" value="<?php echo $course['id']; ?>">
            <button type="submit" class="btn-primary full" <?php echo ($course['slots'] ?? 0) <= 0 ? 'disabled' : ''; ?>>
                <?php echo ($course['slots'] ?? 0) <= 0 ? 'Full' : 'Enroll Now'; ?>
            </button>
        </form>
    </div>
    <?php endforeach; ?>
</div>

<?php include 'includes/footer.php'; ?>
