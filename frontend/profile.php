<?php
require_once 'includes/config.php';
require_auth();

$page_title = 'Profile';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $name = trim($_POST['name'] ?? '');
    $email = trim($_POST['email'] ?? '');

    if (empty($name) || empty($email)) {
        set_flash('error', 'Name and email are required.');
    } else {
        $result = api_call('/users/' . $_SESSION['user']['id'], 'PUT', [
            'name' => $name,
            'email' => $email
        ], $_SESSION['token'] ?? null);

        if ($result['code'] === 200) {
            $_SESSION['user']['name'] = $name;
            $_SESSION['user']['email'] = $email;
            set_flash('success', 'Profile updated successfully.');
        } else {
            set_flash('error', $result['data']['message'] ?? 'Update failed.');
        }
    }
    header('Location: profile.php');
    exit;
}

$user = api_call('/users/' . $_SESSION['user']['id'], 'GET', null, $_SESSION['token'] ?? null);
if ($user['code'] !== 200 || empty($user['data'])) {
    $user['data'] = $_SESSION['user'];
}
?>
<?php include 'includes/header.php'; ?>

<div class="page-header">
    <h1>My Profile</h1>
    <p class="subtitle">Manage your account details</p>
</div>

<div class="profile-layout">
    <div class="profile-card">
        <div class="avatar"><?php echo strtoupper(substr($user['data']['name'] ?? 'U', 0, 1)); ?></div>
        <h2><?php echo htmlspecialchars($user['data']['name'] ?? 'User'); ?></h2>
        <p class="role-badge"><?php echo $user['data']['role'] ?? 'Student'; ?></p>
        <p class="email"><?php echo htmlspecialchars($user['data']['email'] ?? ''); ?></p>
    </div>

    <div class="card">
        <div class="card-header"><h2>Edit Profile</h2></div>
        <form method="POST" action="" class="form">
            <div class="form-group">
                <label for="name">Full Name</label>
                <input type="text" id="name" name="name" value="<?php echo htmlspecialchars($user['data']['name'] ?? ''); ?>" required>
            </div>
            <div class="form-group">
                <label for="email">Email Address</label>
                <input type="email" id="email" name="email" value="<?php echo htmlspecialchars($user['data']['email'] ?? ''); ?>" required>
            </div>
            <div class="form-group">
                <label>Role</label>
                <input type="text" value="<?php echo $user['data']['role'] ?? 'Student'; ?>" disabled class="disabled">
            </div>
            <button type="submit" class="btn-primary">Save Changes</button>
        </form>
    </div>
</div>

<?php include 'includes/footer.php'; ?>
