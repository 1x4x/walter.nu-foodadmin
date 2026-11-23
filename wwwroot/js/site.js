function showTab(name, button) {
  document.querySelectorAll('.admin-tab').forEach(tab => tab.classList.toggle('hidden', tab.id !== 'tab-' + name));
  document.querySelectorAll('.nav-link').forEach(link => link.classList.toggle('active', link.dataset.tab === name));
  if (button) button.classList.add('active');
  if (document.querySelector('.admin-layout')) history.replaceState(null, '', '#' + name);
  window.scrollTo({ top: 0, behavior: 'instant' });
}
function editFood(item) {
  showTab('foods');
  for (const key of ['Id','Name','Description','Price','CategoryId','ImageId','Badge','Allergens','SortOrder']) {
    const input = document.getElementById('food-' + key);
    if (input) input.value = item[key] ?? '';
  }
  document.getElementById('food-Available').checked = !!item.Available;
  document.getElementById('food-Featured').checked = !!item.Featured;
  document.getElementById('food-IsRealFood').checked = item.IsRealFood !== false;
  const gallery = new Set(item.ImageIds || []);
  document.querySelectorAll('#food-ImageIds option').forEach(option => option.selected = gallery.has(option.value));
  document.getElementById('food-form-title').textContent = 'Edit food item';
}
function editCategory(item) {
  showTab('categories');
  for (const key of ['Id','Name','Description','SortOrder']) {
    const input = document.getElementById('cat-' + key);
    if (input) input.value = item[key] ?? '';
  }
  document.getElementById('cat-Visible').checked = !!item.Visible;
}
function resetFood() {
  document.getElementById('food-form').reset();
  document.getElementById('food-Id').value = crypto.randomUUID().replaceAll('-', '');
  document.getElementById('food-form-title').textContent = 'Add or edit food';
}
function resetCategory() {
  document.getElementById('category-form').reset();
  document.getElementById('cat-Id').value = crypto.randomUUID().replaceAll('-', '');
}
function updateClock() {
  const el = document.getElementById('clock');
  if (el) el.textContent = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}
updateClock();
setInterval(updateClock, 15000);

if (document.querySelector('.admin-layout')) {
  const initial = location.hash.replace('#', '');
  if (['overview','foods','categories','images','themes','settings'].includes(initial)) showTab(initial);
  document.querySelectorAll('.theme-option input[type=radio]').forEach(radio => radio.addEventListener('change', () => {
    document.querySelectorAll('.theme-option').forEach(option => option.classList.toggle('selected', !!option.querySelector('input:checked')));
  }));
  document.querySelectorAll('.admin-tab form[method=post]').forEach(form => {
    form.addEventListener('submit', () => {
      const section = form.closest('.admin-tab');
      if (section) sessionStorage.setItem('restaurantVisionLastTab', section.id.replace('tab-', ''));
    });
  });
  const last = sessionStorage.getItem('restaurantVisionLastTab');
  if (!initial && last) showTab(last);
  sessionStorage.removeItem('restaurantVisionLastTab');
}

if (location.pathname.toLowerCase() === '/tv') {
  const shell = document.querySelector('.menu-shell');
  const sections = Array.from(document.querySelectorAll('.category-block'));
  const seconds = Math.max(3, Number(shell?.dataset.rotation) || 12);
  if (shell && sections.length > 1) {
    shell.classList.add('tv-rotate');
    let current = 0;
    sections[0].classList.add('on-screen');
    setInterval(() => {
      sections[current].classList.remove('on-screen');
      current = (current + 1) % sections.length;
      sections[current].classList.add('on-screen');
    }, seconds * 1000);
  }
}