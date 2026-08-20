<?php
require_once 'includes/config.php';

if (isset($_SESSION['user'])) {
    header('Location: dashboard.php');
    exit;
}

$page_title = 'Register';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $name = trim($_POST['name'] ?? '');
    $email = trim($_POST['email'] ?? '');
    $password = $_POST['password'] ?? '';
    $confirm = $_POST['confirm_password'] ?? '';
    $role = $_POST['role'] ?? 'Student';

    if (empty($name) || empty($email) || empty($password)) {
        set_flash('error', 'All fields are required.');
    } elseif ($password !== $confirm) {
        set_flash('error', 'Passwords do not match.');
    } elseif (strlen($password) < 6) {
        set_flash('error', 'Password must be at least 6 characters.');
    } else {
        $result = api_call('/auth/register', 'POST', [
            'name' => $name,
            'email' => $email,
            'password' => $password,
            'role' => $role
        ]);

        if ($result['code'] === 201 || $result['code'] === 200) {
            set_flash('success', 'Account created! Please login.');
            header('Location: login.php');
            exit;
        } else {
            set_flash('error', $result['data']['message'] ?? 'Registration failed.');
        }
    }
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?php echo APP_NAME; ?> — Register</title>
    <link rel="stylesheet" href="assets/css/style.css">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
</head>
<body class="auth-body">
    <?php $flash = get_flash(); if ($flash): ?>
    <div class="flash flash-<?php echo $flash['type']; ?>" id="flash-msg">
        <?php echo htmlspecialchars($flash['message']); ?>
        <span class="flash-close" onclick="this.parentElement.remove()">&times;</span>
    </div>
    <?php endif; ?>

    <div class="auth-card">
        <div class="auth-header">
            <div class="logo large">N</div>
            <h1>Create Account</h1>
            <p>Join <?php echo APP_NAME; ?> today</p>
        </div>
        <form method="POST" action="" class="auth-form">
            <div class="form-group">
                <label for="name">Full Name</label>
                <input type="text" id="name" name="name" placeholder="John Doe" required>
            </div>
            <div class="form-group">
                <label for="email">Email Address</label>
                <input type="email" id="email" name="email" placeholder="you@example.com" required>
            </div>
            <div class="form-group">
                <label for="role">Role</label>
                <select id="role" name="role" required>
                    <option value="Student">Student</option>
                    <option value="Instructor">Instructor</option>
                </select>
            </div>
            <div class="form-row">
                <div class="form-group">
                    <label for="password">Password</label>
                    <input type="password" id="password" name="password" placeholder="••••••••" required>
                </div>
                <div class="form-group">
                    <label for="confirm_password">Confirm</label>
                    <input type="password" id="confirm_password" name="confirm_password" placeholder="••••••••" required>
                </div>
            </div>
            <button type="submit" class="btn-primary full">Create Account</button>
        </form>
        <div class="auth-footer">
            <p>Already have an account? <a href="login.php">Sign in</a></p>
        </div>
    </div>
    <script src="assets/js/main.js"></script>
</body>
</html>
