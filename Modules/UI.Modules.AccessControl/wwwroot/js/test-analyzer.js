// test-analyzer.js - Handle token analysis and display results

document.addEventListener('DOMContentLoaded', function() {
    const analyzeButton = document.getElementById('analyzeToken');
    const clearButton = document.getElementById('clearResults');
    const tokenInput = document.getElementById('jwtToken');
    const workstreamSelect = document.getElementById('workstreamId');
    const resultsDiv = document.getElementById('results');
    const errorAlert = document.getElementById('errorAlert');

    analyzeButton.addEventListener('click', async function() {
        const token = tokenInput.value.trim();
        const workstreamId = workstreamSelect.value;

        if (!token) {
            showError('Please enter a JWT token.');
            return;
        }

        hideError();
        showLoading();

        try {
            const response = await fetch('/Test/DecodeToken', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ token, workstreamId })
            });

            const result = await response.json();

            if (result.success) {
                displayResults(result);
                // Load and display available scenarios for this user/workstream
                await loadAvailableScenarios(token, workstreamId);
            } else {
                showError(result.errorMessage || 'Failed to decode token.');
            }
        } catch (error) {
            showError('Error analyzing token: ' + error.message);
        }
    });

    clearButton.addEventListener('click', function() {
        tokenInput.value = '';
        resultsDiv.style.display = 'none';
        hideError();

        // Hide scenarios and custom test sections
        const scenariosSection = document.getElementById('scenariosSection');
        const customTestSection = document.getElementById('customTestSection');
        const evaluationTraceSection = document.getElementById('evaluationTraceSection');

        if (scenariosSection) scenariosSection.style.display = 'none';
        if (customTestSection) customTestSection.style.display = 'none';
        if (evaluationTraceSection) evaluationTraceSection.style.display = 'none';
    });

    function showLoading() {
        analyzeButton.disabled = true;
        analyzeButton.textContent = 'Analyzing...';
    }

    function showError(message) {
        errorAlert.textContent = message;
        errorAlert.style.display = 'block';
        resultsDiv.style.display = 'none';
        analyzeButton.disabled = false;
        analyzeButton.textContent = 'Analyze Token';
    }

    function hideError() {
        errorAlert.style.display = 'none';
    }

    function displayResults(result) {
        analyzeButton.disabled = false;
        analyzeButton.textContent = 'Analyze Token';
        resultsDiv.style.display = 'block';

        // Display token claims
        displayTokenClaims(result.claims);

        // Display groups and roles
        displayGroups(result.groups);
        displayRoles(result.roles);

        // Display merged attributes
        displayMergedAttributes(result.mergedAttributes);

        // Display user attributes
        displayUserAttributes(result.userAttributes);

        // Display role attributes
        displayRoleAttributes(result.roleAttributes);

        // Display group attributes
        displayGroupAttributes(result.groupAttributes);

        // Display policies
        displayPolicies(result.applicablePolicies);

        // Display ABAC rules
        displayAbacRules(result.applicableAbacRules);

        // Display rule groups
        displayRuleGroups(result.applicableRuleGroups);
    }

    function displayTokenClaims(claims) {
        const container = document.getElementById('tokenClaims');
        container.innerHTML = '';

        for (const [key, value] of Object.entries(claims)) {
            container.innerHTML += `
                <dt class="col-sm-4 small">${escapeHtml(key)}</dt>
                <dd class="col-sm-8 small"><code>${escapeHtml(value)}</code></dd>
            `;
        }
    }

    function displayGroups(groups) {
        const container = document.getElementById('groupsList');
        if (groups && groups.length > 0) {
            container.innerHTML = groups.map(g => `<span class="badge bg-primary me-1">${escapeHtml(g)}</span>`).join('');
        } else {
            container.innerHTML = '<em class="text-muted">No groups</em>';
        }
    }

    function displayRoles(roles) {
        const container = document.getElementById('rolesList');
        if (roles && roles.length > 0) {
            container.innerHTML = roles.map(r => `<span class="badge bg-success me-1">${escapeHtml(r)}</span>`).join('');
        } else {
            container.innerHTML = '<em class="text-muted">No roles</em>';
        }
    }

    function displayMergedAttributes(attributes) {
        const tbody = document.querySelector('#mergedAttributesTable tbody');
        tbody.innerHTML = '';

        if (!attributes || Object.keys(attributes).length === 0) {
            tbody.innerHTML = '<tr><td colspan="3" class="text-muted text-center">No attributes</td></tr>';
            return;
        }

        for (const [name, attr] of Object.entries(attributes)) {
            let badgeClass = 'bg-secondary';
            if (attr.source === 'User') badgeClass = 'bg-success';
            else if (attr.source === 'Role') badgeClass = 'bg-warning text-dark';
            else if (attr.source === 'Group') badgeClass = 'bg-info';

            tbody.innerHTML += `
                <tr>
                    <td>${escapeHtml(name)}</td>
                    <td><code>${escapeHtml(attr.value)}</code></td>
                    <td><span class="badge ${badgeClass}">${escapeHtml(attr.source)}</span> ${escapeHtml(attr.entityId || '')}</td>
                </tr>
            `;
        }
    }

    function displayUserAttributes(attributes) {
        const tbody = document.querySelector('#userAttributesTable tbody');
        tbody.innerHTML = '';

        if (!attributes || attributes.length === 0) {
            tbody.innerHTML = '<tr><td colspan="2" class="text-muted text-center">No user attributes</td></tr>';
            return;
        }

        attributes.forEach(attr => {
            tbody.innerHTML += `
                <tr>
                    <td>${escapeHtml(attr.attributeName)}</td>
                    <td><code>${escapeHtml(attr.value)}</code></td>
                </tr>
            `;
        });
    }

    function displayRoleAttributes(attributes) {
        const tbody = document.querySelector('#roleAttributesTable tbody');
        tbody.innerHTML = '';

        if (!attributes || attributes.length === 0) {
            tbody.innerHTML = '<tr><td colspan="3" class="text-muted text-center">No role attributes</td></tr>';
            return;
        }

        attributes.forEach(attr => {
            tbody.innerHTML += `
                <tr>
                    <td><code>${escapeHtml(attr.entityId)}</code></td>
                    <td>${escapeHtml(attr.attributeName)}</td>
                    <td><code>${escapeHtml(attr.value)}</code></td>
                </tr>
            `;
        });
    }

    function displayGroupAttributes(attributes) {
        const tbody = document.querySelector('#groupAttributesTable tbody');
        tbody.innerHTML = '';

        if (!attributes || attributes.length === 0) {
            tbody.innerHTML = '<tr><td colspan="3" class="text-muted text-center">No group attributes</td></tr>';
            return;
        }

        attributes.forEach(attr => {
            tbody.innerHTML += `
                <tr>
                    <td><code>${escapeHtml(attr.entityId)}</code></td>
                    <td>${escapeHtml(attr.attributeName)}</td>
                    <td><code>${escapeHtml(attr.value)}</code></td>
                </tr>
            `;
        });
    }

    function displayPolicies(policies) {
        const tbody = document.querySelector('#policiesTable tbody');
        tbody.innerHTML = '';

        if (!policies || policies.length === 0) {
            tbody.innerHTML = '<tr><td colspan="3" class="text-muted text-center">No applicable policies</td></tr>';
            return;
        }

        policies.forEach(policy => {
            let typeClass = policy.policyType === 'p' ? 'primary' : policy.policyType === 'g' ? 'success' : 'info';
            tbody.innerHTML += `
                <tr>
                    <td><span class="badge bg-${typeClass}">${escapeHtml(policy.policyType)}</span></td>
                    <td><code>${escapeHtml(policy.displayText)}</code></td>
                    <td>${escapeHtml(policy.workstreamId)}</td>
                </tr>
            `;
        });
    }

    function displayAbacRules(rules) {
        const tbody = document.querySelector('#abacRulesTable tbody');
        tbody.innerHTML = '';

        if (!rules || rules.length === 0) {
            tbody.innerHTML = '<tr><td colspan="2" class="text-muted text-center">No ABAC rules</td></tr>';
            return;
        }

        rules.forEach(rule => {
            tbody.innerHTML += `
                <tr>
                    <td><span class="badge bg-dark">${escapeHtml(rule.ruleType)}</span></td>
                    <td><code>${escapeHtml(rule.displayText)}</code></td>
                </tr>
            `;
        });
    }

    function displayRuleGroups(groups) {
        const tbody = document.querySelector('#ruleGroupsTable tbody');
        tbody.innerHTML = '';

        if (!groups || groups.length === 0) {
            tbody.innerHTML = '<tr><td colspan="3" class="text-muted text-center">No rule groups</td></tr>';
            return;
        }

        groups.forEach(group => {
            tbody.innerHTML += `
                <tr>
                    <td>${escapeHtml(group.groupName)}</td>
                    <td><span class="badge bg-secondary">${escapeHtml(group.logicOperator)}</span></td>
                    <td>${group.ruleCount} rules</td>
                </tr>
            `;
        });
    }

    function escapeHtml(text) {
        if (text === null || text === undefined) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return String(text).replace(/[&<>"']/g, m => map[m]);
    }

    // Load and display dynamic scenarios based on user permissions
    async function loadAvailableScenarios(token, workstreamId) {
        try {
            const response = await fetch('/Test/GetAvailableScenarios', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ token, workstreamId })
            });

            const result = await response.json();

            if (result.success && result.scenarios) {
                displayDynamicScenarios(result.scenarios, token, workstreamId);
            } else {
                console.error('Failed to load scenarios:', result.errorMessage);
            }
        } catch (error) {
            console.error('Error loading scenarios:', error);
        }
    }

    function displayDynamicScenarios(scenarios, token, workstreamId) {
        // Show the scenarios and custom test sections
        const scenariosSection = document.getElementById('scenariosSection');
        const customTestSection = document.getElementById('customTestSection');

        if (scenariosSection) scenariosSection.style.display = 'block';
        if (customTestSection) customTestSection.style.display = 'block';

        // Find the scenarios container by looking for the specific card structure
        const allCards = document.querySelectorAll('.card');
        let scenariosCard = null;

        for (const card of allCards) {
            const header = card.querySelector('.card-header.bg-primary.text-white h5');
            if (header && header.textContent.includes('Interactive Test Scenarios')) {
                scenariosCard = card;
                break;
            }
        }

        if (!scenariosCard) {
            console.error('Scenarios card not found');
            return;
        }

        const scenariosContainer = scenariosCard.querySelector('.card-body .row');
        if (!scenariosContainer) {
            console.error('Scenarios container not found');
            return;
        }

        // Clear existing scenarios
        scenariosContainer.innerHTML = '';

        if (!scenarios || scenarios.length === 0) {
            scenariosContainer.innerHTML = '<div class="col-md-12"><p class="text-muted">No scenarios available for this user in this workstream.</p></div>';
            return;
        }

        // Create scenario cards for each resource
        scenarios.forEach(scenario => {
            const scenarioCard = document.createElement('div');
            scenarioCard.className = 'col-md-6 mb-3';
            scenarioCard.innerHTML = `
                <div class="border p-3 rounded">
                    <h6>${escapeHtml(scenario.name)}</h6>
                    <p class="small text-muted">${escapeHtml(scenario.description)}</p>
                    <p class="small"><strong>Available Actions:</strong> ${scenario.availableActions.map(a => `<code>${escapeHtml(a)}</code>`).join(', ')}</p>
                    <button class="btn btn-sm btn-outline-primary scenario-button" data-scenario="${escapeHtml(scenario.id)}">
                        Test ${escapeHtml(scenario.resource)}
                    </button>
                </div>
            `;
            scenariosContainer.appendChild(scenarioCard);
        });

        // Re-initialize scenario buttons
        initializeScenarioButtons();
    }

    // Section 5: Interactive Test Scenarios
    function initializeScenarioButtons() {
        const scenarioButtons = document.querySelectorAll('.scenario-button');

        scenarioButtons.forEach(button => {
            // Remove existing listeners to avoid duplicates
            const newButton = button.cloneNode(true);
            button.parentNode.replaceChild(newButton, button);

            newButton.addEventListener('click', async function() {
                const scenarioName = this.dataset.scenario;
                const token = tokenInput.value.trim();
                const workstreamId = workstreamSelect.value;

                if (!token) {
                    showError('Please analyze a token first before running scenarios.');
                    return;
                }

                newButton.disabled = true;
                newButton.textContent = 'Running...';

                try {
                    const response = await fetch('/Test/RunScenario', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            token: token,
                            workstreamId: workstreamId,
                            scenarioName: scenarioName
                        })
                    });

                    const result = await response.json();

                    if (result.success) {
                        displayScenarioResult(result, newButton);
                    } else {
                        showError(result.errorMessage || 'Scenario execution failed.');
                        newButton.disabled = false;
                        newButton.textContent = newButton.dataset.originalText || 'Run';
                    }
                } catch (error) {
                    showError('Error running scenario: ' + error.message);
                    newButton.disabled = false;
                    newButton.textContent = newButton.dataset.originalText || 'Run';
                }
            });

            // Store original button text
            newButton.dataset.originalText = newButton.textContent;
        });
    }

    function displayScenarioResult(result, button) {
        button.disabled = false;

        // Update button text with pass/fail indicator
        if (result.isAuthorized) {
            button.textContent = '✅ Passed';
            button.classList.remove('btn-outline-danger');
            button.classList.add('btn-outline-success');
        } else {
            button.textContent = '❌ Failed';
            button.classList.remove('btn-outline-success');
            button.classList.add('btn-outline-danger');
        }

        // Display evaluation trace
        displayEvaluationTrace(result.evaluationTrace);

        // Show decision badge
        const resultContainer = button.closest('.card-body');
        const existingBadge = resultContainer.querySelector('.scenario-result-badge');
        if (existingBadge) {
            existingBadge.remove();
        }

        const badge = document.createElement('span');
        badge.className = 'badge ms-2 scenario-result-badge';
        badge.classList.add(result.isAuthorized ? 'bg-success' : 'bg-danger');
        badge.textContent = result.decision;
        button.parentElement.appendChild(badge);
    }

    // Section 6: Custom Test Builder
    function initializeCustomTestBuilder() {
        const runCustomTestButton = document.getElementById('runCustomTest');
        const customResourceInput = document.getElementById('customResource');
        const customActionInput = document.getElementById('customAction');
        const customEntityJsonInput = document.getElementById('customEntityJson');

        if (!runCustomTestButton) return;

        runCustomTestButton.addEventListener('click', async function() {
            const token = tokenInput.value.trim();
            const workstreamId = workstreamSelect.value;
            const resource = customResourceInput.value.trim();
            const action = customActionInput.value.trim();
            const mockEntityJson = customEntityJsonInput.value.trim();

            if (!token) {
                showError('Please analyze a token first before running custom tests.');
                return;
            }

            if (!resource || !action) {
                showError('Please provide both resource and action for the custom test.');
                return;
            }

            // Validate JSON if provided
            if (mockEntityJson) {
                try {
                    JSON.parse(mockEntityJson);
                } catch (e) {
                    showError('Invalid JSON in mock entity field: ' + e.message);
                    return;
                }
            }

            runCustomTestButton.disabled = true;
            runCustomTestButton.textContent = 'Testing...';

            try {
                const response = await fetch('/Test/CheckAuthorization', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        token: token,
                        workstreamId: workstreamId,
                        resource: resource,
                        action: action,
                        mockEntityJson: mockEntityJson || null,
                        testDescription: `Custom test: ${resource}:${action}`
                    })
                });

                const result = await response.json();

                if (result.success) {
                    displayCustomTestResult(result);
                } else {
                    showError(result.errorMessage || 'Custom test failed.');
                }
            } catch (error) {
                showError('Error running custom test: ' + error.message);
            } finally {
                runCustomTestButton.disabled = false;
                runCustomTestButton.textContent = 'Run Custom Test';
            }
        });
    }

    function displayCustomTestResult(result) {
        // Display evaluation trace
        displayEvaluationTrace(result.evaluationTrace);

        // Show result in a prominent alert
        const customTestSection = document.getElementById('customEntityJson').closest('.card-body');
        let resultAlert = customTestSection.querySelector('.custom-test-result-alert');

        if (!resultAlert) {
            resultAlert = document.createElement('div');
            resultAlert.className = 'alert custom-test-result-alert mt-3';
            customTestSection.appendChild(resultAlert);
        }

        resultAlert.className = 'alert custom-test-result-alert mt-3';
        resultAlert.classList.add(result.isAuthorized ? 'alert-success' : 'alert-danger');
        resultAlert.innerHTML = `
            <h5 class="alert-heading">
                ${result.isAuthorized ? '✅' : '❌'} ${result.decision}
            </h5>
            <p class="mb-0">${result.isAuthorized ? 'Authorization granted' : 'Authorization denied'}</p>
        `;

        // Scroll to evaluation trace
        document.getElementById('evaluationTraceSection')?.scrollIntoView({ behavior: 'smooth' });
    }

    // Section 7: Evaluation Trace Display
    function displayEvaluationTrace(trace) {
        const traceContainer = document.getElementById('evaluationTrace');
        const traceSection = document.getElementById('evaluationTraceSection');

        if (!traceContainer || !trace || trace.length === 0) {
            if (traceSection) traceSection.style.display = 'none';
            return;
        }

        // Show the section
        if (traceSection) traceSection.style.display = 'block';

        // Clear previous trace
        traceContainer.innerHTML = '';

        // Sort by order
        const sortedTrace = [...trace].sort((a, b) => a.order - b.order);

        // Icon mapping for different step types
        const iconMap = {
            'JWT Claims Extracted': '🔑',
            'User Context Loaded': '👤',
            'Groups Resolved': '👥',
            'Roles Resolved': '🎭',
            'Attributes Merged': '📋',
            'RBAC Check': '🛡️',
            'ABAC Evaluation': '🎯',
            'Final Decision': '✅'
        };

        sortedTrace.forEach((step, index) => {
            const stepDiv = document.createElement('div');
            stepDiv.className = 'evaluation-step mb-3 p-3 border rounded';

            // Determine icon based on step name
            let icon = '📍'; // default
            for (const [key, value] of Object.entries(iconMap)) {
                if (step.stepName.includes(key)) {
                    icon = value;
                    break;
                }
            }

            // Result badge
            let resultClass = 'secondary';
            if (step.result === 'Pass') resultClass = 'success';
            else if (step.result === 'Fail') resultClass = 'danger';
            else if (step.result === 'Skip') resultClass = 'warning';

            stepDiv.innerHTML = `
                <div class="d-flex align-items-start">
                    <div class="me-3" style="font-size: 1.5rem;">${icon}</div>
                    <div class="flex-grow-1">
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <h6 class="mb-0">
                                <span class="badge bg-secondary me-2">${index + 1}</span>
                                ${escapeHtml(step.stepName)}
                            </h6>
                            <span class="badge bg-${resultClass}">${escapeHtml(step.result)}</span>
                        </div>
                        <p class="mb-1 text-muted">${escapeHtml(step.description)}</p>
                        ${step.details ? `<div class="mt-2"><code class="small">${escapeHtml(step.details)}</code></div>` : ''}
                    </div>
                </div>
            `;

            traceContainer.appendChild(stepDiv);
        });
    }

    // Initialize all interactive features when DOM is ready
    initializeScenarioButtons();
    initializeCustomTestBuilder();
});
