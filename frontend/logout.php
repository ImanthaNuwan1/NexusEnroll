<?php
require_once 'includes/config.php';

// Optional: notify C# backend of logout
if (isset($_SESSION['token'])) {
    api_call('/auth/logout', 'POST', null, $_SESSION['token']);
}

session_destroy();
header('Location: login.php');
exit;
?>
