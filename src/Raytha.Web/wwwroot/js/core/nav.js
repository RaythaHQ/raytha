/**
 * Navigation Utilities
 * Handles active state highlighting for sidebar and navigation menus.
 */

/**
 * Set active nav item based on current route
 * @param {string} routeName - Current route name (e.g., "Admin.Users.Index")
 */
export const setActiveNavItem = (routeName) => {
  if (!routeName) return;
  
  // Find nav items matching the route
  const navItems = document.querySelectorAll('[data-nav-route]');
  
  navItems.forEach(item => {
    const itemRoute = item.getAttribute('data-nav-route');
    
    if (itemRoute === routeName) {
      item.classList.add('active');
      
      // Also mark parent items as active
      let parent = item.closest('.nav-item, .list-group-item');
      while (parent) {
        parent.classList.add('active');
        parent = parent.parentElement?.closest('.nav-item, .list-group-item');
      }
    } else {
      item.classList.remove('active');
    }
  });
};

/**
 * Set active menu item based on data-active-menu attribute
 * Reads from body[data-active-menu] and applies .active to matching sidebar items
 */
export const highlightActiveMenu = () => {
  const activeMenu = document.body.getAttribute('data-active-menu');
  if (!activeMenu) return;
  
  // Find sidebar links with matching data-menu attribute
  const menuItems = document.querySelectorAll('[data-menu]');
  
  menuItems.forEach(item => {
    const menuName = item.getAttribute('data-menu');
    
    if (menuName === activeMenu) {
      item.classList.add('active');
      
      // Expand parent collapse if exists
      const collapse = item.closest('.collapse');
      if (collapse) {
        collapse.classList.add('show');
      }
    } else {
      item.classList.remove('active');
    }
  });
};

/**
 * Highlight current page in sidebar based on href matching
 * Falls back to simple URL matching if no data attributes
 */
export const highlightCurrentPage = () => {
  const currentPath = window.location.pathname;
  
  const navLinks = document.querySelectorAll('.sidebar a, .nav-link');
  
  navLinks.forEach(link => {
    const linkPath = new URL(link.href, window.location.origin).pathname;
    
    if (linkPath === currentPath) {
      link.classList.add('active');
      
      // Expand parent menu
      const parentCollapse = link.closest('.collapse');
      if (parentCollapse) {
        parentCollapse.classList.add('show');
      }
      
      // Mark parent nav item
      const parentNavItem = link.closest('.nav-item');
      if (parentNavItem) {
        parentNavItem.classList.add('active');
      }
    }
  });
};

/**
 * Initialize navigation highlighting
 * Call this on page load
 */
export const initNavigation = () => {
  // Try data-active-menu first (set by ViewData in Razor Pages)
  highlightActiveMenu();
  
  // Fall back to current page URL matching
  if (!document.body.getAttribute('data-active-menu')) {
    highlightCurrentPage();
  }
};

/**
 * Handle sidebar collapse/expand state persistence
 * Saves state to localStorage
 */
export const initSidebarPersistence = () => {
  const sidebar = document.querySelector('.sidebar');
  if (!sidebar) return;
  
  const STORAGE_KEY = 'raytha_sidebar_state';
  
  // Restore state from localStorage
  const savedState = localStorage.getItem(STORAGE_KEY);
  if (savedState === 'collapsed') {
    sidebar.classList.add('collapsed');
  }
  
  // Listen for toggle events
  const toggleBtn = document.querySelector('[data-sidebar-toggle]');
  if (toggleBtn) {
    toggleBtn.addEventListener('click', () => {
      sidebar.classList.toggle('collapsed');
      
      const isCollapsed = sidebar.classList.contains('collapsed');
      localStorage.setItem(STORAGE_KEY, isCollapsed ? 'collapsed' : 'expanded');
    });
  }
};

/**
 * Expand sidebar section
 * @param {string} sectionId - ID of collapse element to expand
 */
export const expandSidebarSection = (sectionId) => {
  const section = document.getElementById(sectionId);
  if (section && section.classList.contains('collapse')) {
    const bsCollapse = new bootstrap.Collapse(section, { toggle: false });
    bsCollapse.show();
  }
};

/**
 * Collapse sidebar section
 * @param {string} sectionId - ID of collapse element to collapse
 */
export const collapseSidebarSection = (sectionId) => {
  const section = document.getElementById(sectionId);
  if (section && section.classList.contains('collapse')) {
    const bsCollapse = bootstrap.Collapse.getInstance(section);
    if (bsCollapse) {
      bsCollapse.hide();
    }
  }
};

