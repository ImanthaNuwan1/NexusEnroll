<?php
require_once 'includes/config.php';

// Redirect if already logged in
if (isset($_SESSION['user'])) {
    header('Location: dashboard.php');
    exit;
}

$page_title = 'Login';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $email = trim($_POST['email'] ?? '');
    $password = $_POST['password'] ?? '';

    if (empty($email) || empty($password)) {
        set_flash('error', 'Email and password are required.');
    } else {
        // ── Connect to C# Backend ──
        $result = api_call('/auth/login', 'POST', [
            'email' => $email,
            'password' => $password
        ]);

        if ($result['code'] === 200 && isset($result['data']['token'])) {
            $_SESSION['user'] = $result['data']['user'];
            $_SESSION['token'] = $result['data']['token'];
            set_flash('success', 'Welcome back, ' . htmlspecialchars($result['data']['user']['name']) . '!');
            header('Location: dashboard.php');
            exit;
        } else {
            // Fallback: local demo auth (remove when C# backend is ready)
            if ($email === 'demo@nexus.com' && $password === 'demo123') {
                $_SESSION['user'] = [
                    'id' => 1,
                    'name' => 'Demo User',
                    'email' => $email,
                    'role' => 'Student'
                ];
                $_SESSION['token'] = 'demo-token';
                set_flash('success', 'Welcome back, Demo User!');
                header('Location: dashboard.php');
                exit;
            }
            set_flash('error', $result['data']['message'] ?? 'Invalid credentials.');
        }
    }
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?php echo APP_NAME; ?> — Login</title>
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
            <h1><?php echo APP_NAME; ?></h1>
            <p>Enrollment Management Portal</p>
        </div>
        <form method="POST" action="" class="auth-form">
            <div class="form-group">
                <label for="email">Email Address</label>
                <input type="email" id="email" name="email" placeholder="you@example.com" required autofocus>
            </div>
            <div class="form-group">
                <label for="password">Password</label>
                <input type="password" id="password" name="password" placeholder="••••••••" required>
            </div>
            <button type="submit" class="btn-primary full">Sign In</button>
        </form>
        <div class="auth-footer">
            <p>Offline demo: <code>demo@nexus.com</code> / <code>demo123</code></p>
            <p>Backend running? Try <code>john.doe@nexus.edu</code> (any password) — see <code>backend/Program.cs</code> for more seeded accounts.</p>
            <p>Don't have an account? <a href="register.php">Register here</a></p>
        </div>
    </div>
    <script src="assets/js/main.js"></script>
</body>
</html>
