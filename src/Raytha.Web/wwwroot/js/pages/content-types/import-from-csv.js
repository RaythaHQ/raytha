/**
 * Content Types - Import from CSV Page
 * Handles background task polling and progress display for CSV imports
 */

import { ready } from '/js/core/events.js';

/**
 * Initialize the background task progress tracker
 * @param {Object} config - Configuration object
 * @param {string} config.pathBase - Base path for URLs
 * @param {string} config.taskId - Background job ID to track
 */
function initProgressTracker(config) {
  const progressBar = document.getElementById('progress-bar');
  const statusInfo = document.getElementById('status-info');
  const backToView = document.getElementById('back-to-view');

  if (!progressBar || !statusInfo || !config.taskId) {
    return; // Not on progress view
  }

  const { pathBase, taskId } = config;
  const currentPath = window.location.pathname + '?handler=Status&id=' + encodeURIComponent(taskId);

  let pollHandle = null;
  let currentTaskStep = 0;

  /**
   * Set progress bar percentage
   * @param {number} pct - Percentage (0-100)
   */
  function setProgress(pct) {
    const clamped = Math.max(0, Math.min(100, Number(pct) || 0));
    progressBar.style.width = clamped + '%';
    progressBar.setAttribute('aria-valuenow', String(clamped));
  }

  /**
   * Append status message to list
   * @param {string} text - Status message
   */
  function appendItem(text) {
    const li = document.createElement('li');
    li.className = 'list-group-item';
    li.textContent = text;
    statusInfo.appendChild(li);
  }

  /**
   * Show the back link
   */
  function showBackLink() {
    if (backToView) backToView.style.display = '';
  }

  /**
   * Stop polling for updates
   */
  function stopPolling() {
    if (pollHandle) {
      clearInterval(pollHandle);
      pollHandle = null;
    }
  }

  /**
   * Try to redirect if statusInfo contains media item JSON (error CSV)
   * @param {string} raw - Raw status info string
   */
  function tryRedirectIfMediaInfo(raw) {
    if (!raw) return;
    try {
      const mediaItem = JSON.parse(raw);
      if (mediaItem && mediaItem.ObjectKey) {
        const url = `${pathBase}/raytha/media-items/objectkey/${mediaItem.ObjectKey}`;
        window.location.href = url;
      }
    } catch (_) {
      // statusInfo wasn't JSON; ignore
    }
  }

  /**
   * Fetch and update task status
   */
  async function refresh() {
    try {
      const res = await fetch(currentPath, { cache: 'no-store' });
      if (!res.ok) throw new Error('HTTP ' + res.status);
      const task = await res.json();

      const status = task?.status?.developerName;
      const step = task?.taskStep;
      const pct = task?.percentComplete ?? 0;

      if (status === 'processing' || status === 'enqueued') {
        if (currentTaskStep !== step) {
          setProgress(pct);

          if (task?.statusInfo) {
            appendItem(task.statusInfo);
          }

          currentTaskStep = step;
        }
      } else if (status === 'error') {
        progressBar.classList.remove('bg-success');
        progressBar.classList.add('bg-danger');
        appendItem('An error has occurred: ' + (task?.errorMessage || 'Unknown error'));
        setProgress(100);
        showBackLink();
        stopPolling();
      } else if (status === 'complete') {
        progressBar.classList.remove('bg-danger');
        progressBar.classList.add('bg-success');

        if (task?.statusInfo) appendItem(task.statusInfo);
        appendItem('Import complete');

        setProgress(100);
        showBackLink();
        stopPolling();

        // Optional redirect if statusInfo contains error CSV media JSON
        tryRedirectIfMediaInfo(task?.statusInfo);
      }
    } catch (err) {
      console.error('Status refresh failed:', err);
      // Don't stop polling on transient failures
    }
  }

  // Start polling immediately
  refresh();
  pollHandle = setInterval(refresh, 1000);

  // Be nice to the browser when tab is hidden
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
      stopPolling();
    } else if (!pollHandle) {
      refresh();
      pollHandle = setInterval(refresh, 1000);
    }
  });
}

// Initialize when DOM is ready
ready(() => {
  const configElement = document.getElementById('import-progress-config');
  if (configElement) {
    const config = {
      pathBase: configElement.dataset.pathBase || '',
      taskId: configElement.dataset.taskId || ''
    };
    initProgressTracker(config);
  }
});

