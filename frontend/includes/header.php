<?php require_once 'config.php'; ?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?php echo APP_NAME; ?> — <?php echo $page_title ?? 'Portal'; ?></title>
    <link rel="stylesheet" href="assets/css/style.css">
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
</head>
<body>
    <?php $flash = get_flash(); if ($flash): ?>
    <div class="flash flash-<?php echo $flash['type']; ?>" id="flash-msg">
        <?php echo htmlspecialchars($flash['message']); ?>
        <span class="flash-close" onclick="this.parentElement.remove()">&times;</span>
    </div>
    <?php endif; ?>

    <?php if (isset($_SESSION['user'])): ?>
    <nav class="navbar">
        <div class="nav-brand">
            <div class="logo">N</div>
            <span class="brand-text"><?php echo APP_NAME; ?></span>
        </div>
        <div class="nav-links">
            <a href="dashboard.php" class="<?php echo basename($_SERVER['PHP_SELF'])=='dashboard.php'?'active':''; ?>">Dashboard</a>
            <a href="enroll.php" class="<?php echo basename($_SERVER['PHP_SELF'])=='enroll.php'?'active':''; ?>">Enroll</a>
            <a href="my-enrollments.php" class="<?php echo basename($_SERVER['PHP_SELF'])=='my-enrollments.php'?'active':''; ?>">My Enrollments</a>
            <?php if ($_SESSION['user']['role'] === 'Admin'): ?>
            <a href="admin.php" class="<?php echo basename($_SERVER['PHP_SELF'])=='admin.php'?'active':''; ?>">Admin</a>
            <?php endif; ?>
            <a href="profile.php" class="<?php echo basename($_SERVER['PHP_SELF'])=='profile.php'?'active':''; ?>">Profile</a>
            <a href="logout.php" class="btn-logout">Logout</a>
        </div>
        <button class="nav-toggle" onclick="document.querySelector('.nav-links').classList.toggle('open')">☰</button>
    </nav>
    <?php endif; ?>
    <main class="main-content">
