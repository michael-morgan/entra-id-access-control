// ABAC Rule Configuration Builder
// Provides a dynamic form interface for building ABAC rule configurations

class AbacRuleBuilder {
    constructor(workstreamId, ruleTypeSelectId, configTextareaId, formContainerId) {
        this.workstreamId = workstreamId;
        this.ruleTypeSelect = document.getElementById(ruleTypeSelectId);
        this.configTextarea = document.getElementById(configTextareaId);
        this.formContainer = document.getElementById(formContainerId);
        this.attributeSchemas = { user: [], group: [], role: [], resource: [] };
        this.currentConfig = {};

        // Note: init() must be called explicitly after construction to properly await schema loading
    }

    async init() {
        // Load attribute schemas for all levels
        await Promise.all([
            this.loadAttributeSchemas('User'),
            this.loadAttributeSchemas('Group'),
            this.loadAttributeSchemas('Role'),
            this.loadAttributeSchemas('Resource')
        ]);

        // Set up event listeners
        this.ruleTypeSelect.addEventListener('change', () => this.onRuleTypeChange());

        // If there's existing configuration, parse and display it
        if (this.configTextarea.value && this.configTextarea.value.trim() !== '') {
            try {
                const existingConfig = JSON.parse(this.configTextarea.value);
                this.currentConfig = existingConfig;
                this.renderForm(this.ruleTypeSelect.value);
                // Note: populateFormFromConfig is called inside renderForm, which will
                // populate the form fields and then call updateConfiguration() to rebuild
                // the JSON. We don't need to manually set the textarea here.
            } catch (e) {
                console.error('Invalid existing configuration JSON', e);
            }
        }
    }

    async loadAttributeSchemas(level) {
        try {
            const response = await fetch(`/api/attribute-schemas?workstreamId=${this.workstreamId}&attributeLevel=${level}`);
            if (response.ok) {
                this.attributeSchemas[level.toLowerCase()] = await response.json();
            }
        } catch (error) {
            console.error(`Failed to load ${level} attribute schemas:`, error);
        }
    }

    onRuleTypeChange() {
        const ruleType = this.ruleTypeSelect.value;
        if (ruleType) {
            this.currentConfig = {};
            this.renderForm(ruleType);
        } else {
            this.formContainer.innerHTML = '';
        }
    }

    renderForm(ruleType) {
        this.formContainer.innerHTML = '';

        switch (ruleType) {
            case 'AttributeComparison':
                this.renderAttributeComparisonForm();
                break;
            case 'PropertyMatch':
                this.renderPropertyMatchForm();
                break;
            case 'ValueRange':
                this.renderValueRangeForm();
                break;
            case 'TimeRestriction':
                this.renderTimeRestrictionForm();
                break;
            case 'AttributeValue':
                this.renderAttributeValueForm();
                break;
        }
    }

    renderAttributeComparisonForm() {
        const html = `
            <div class="card">
                <div class="card-header">
                    <h6 class="mb-0">Attribute Comparison Configuration</h6>
                </div>
                <div class="card-body">
                    <p class="text-muted">Compare a user/group/role attribute with an entity property</p>

                    <div class="row mb-3">
                        <div class="col-md-6">
                            <label class="form-label">Left Side (Attribute)</label>
                            <select class="form-control" id="leftAttributeLevel">
                                <option value="">-- Select Level --</option>
                                <option value="user">User Attribute</option>
                                <option value="group">Group Attribute</option>
                                <option value="role">Role Attribute</option>
                            </select>
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Attribute Name</label>
                            <select class="form-control" id="leftAttributeName" disabled>
                                <option value="">-- Select level first --</option>
                            </select>
                        </div>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Operator</label>
                        <select class="form-control" id="comparisonOperator">
                            <option value="equals">Equals (==)</option>
                            <option value="notEquals">Not Equals (!=)</option>
                            <option value="greaterThan">Greater Than (&gt;)</option>
                            <option value="greaterThanOrEqual">Greater Than or Equal (&gt;=)</option>
                            <option value="lessThan">Less Than (&lt;)</option>
                            <option value="lessThanOrEqual">Less Than or Equal (&lt;=)</option>
                            <option value="contains">Contains</option>
                            <option value="startsWith">Starts With</option>
                            <option value="endsWith">Ends With</option>
                        </select>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Right Side (Entity Property)</label>
                        <input type="text" class="form-control" id="rightProperty"
                               placeholder="e.g., claim.department, document.region" />
                        <small class="form-text text-muted">The property path on the entity being checked</small>
                    </div>
                </div>
            </div>
        `;

        this.formContainer.innerHTML = html;
        this.attachAttributeComparisonListeners();
        this.populateFormFromConfig();
    }

    attachAttributeComparisonListeners() {
        const levelSelect = document.getElementById('leftAttributeLevel');
        const attrSelect = document.getElementById('leftAttributeName');
        const operatorSelect = document.getElementById('comparisonOperator');
        const rightInput = document.getElementById('rightProperty');

        levelSelect.addEventListener('change', () => {
            const level = levelSelect.value;
            attrSelect.disabled = !level;

            if (level) {
                const schemas = this.attributeSchemas[level];
                attrSelect.innerHTML = '<option value="">-- Select attribute --</option>' +
                    schemas.map(s => `<option value="${s.attributeName}">${s.attributeDisplayName}</option>`).join('');
            } else {
                attrSelect.innerHTML = '<option value="">-- Select level first --</option>';
            }

            this.updateConfiguration();
        });

        attrSelect.addEventListener('change', () => this.updateConfiguration());
        operatorSelect.addEventListener('change', () => this.updateConfiguration());
        rightInput.addEventListener('input', () => this.updateConfiguration());
    }

    renderPropertyMatchForm() {
        const html = `
            <div class="card">
                <div class="card-header">
                    <h6 class="mb-0">Property Match Configuration</h6>
                </div>
                <div class="card-body">
                    <p class="text-muted">Match a user/group/role attribute against a resource property</p>

                    <div class="row mb-3">
                        <div class="col-md-6">
                            <label class="form-label">User Attribute Level</label>
                            <select class="form-control" id="pmAttributeLevel">
                                <option value="">-- Select Level --</option>
                                <option value="user">User Attribute</option>
                                <option value="group">Group Attribute</option>
                                <option value="role">Role Attribute</option>
                            </select>
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Attribute Name</label>
                            <select class="form-control" id="pmAttributeName" disabled>
                                <option value="">-- Select level first --</option>
                            </select>
                        </div>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Operator</label>
                        <select class="form-control" id="pmOperator">
                            <option value="==">Equals (==)</option>
                            <option value="!=">Not Equals (!=)</option>
                            <option value="in">In List</option>
                            <option value="notIn">Not In List</option>
                        </select>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Resource Property</label>
                        <input type="text" class="form-control" id="pmResourceProperty"
                               placeholder="e.g., Region, Department, Status" />
                        <small class="form-text text-muted">The property on the resource to compare against</small>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Allow Wildcard (Optional)</label>
                        <input type="text" class="form-control" id="pmAllowWildcard"
                               placeholder="e.g., ALL, *, ANY" />
                        <small class="form-text text-muted">Wildcard value that grants universal access (leave empty for none)</small>
                    </div>
                </div>
            </div>
        `;

        this.formContainer.innerHTML = html;
        this.attachPropertyMatchListeners();
        this.populateFormFromConfig();
    }

    attachPropertyMatchListeners() {
        const levelSelect = document.getElementById('pmAttributeLevel');
        const attrSelect = document.getElementById('pmAttributeName');
        const operatorSelect = document.getElementById('pmOperator');
        const resourcePropInput = document.getElementById('pmResourceProperty');
        const wildcardInput = document.getElementById('pmAllowWildcard');

        levelSelect.addEventListener('change', () => {
            const level = levelSelect.value;
            attrSelect.disabled = !level;

            if (level) {
                const schemas = this.attributeSchemas[level];
                attrSelect.innerHTML = '<option value="">-- Select attribute --</option>' +
                    schemas.map(s => `<option value="${s.attributeName}">${s.attributeDisplayName}</option>`).join('');
            } else {
                attrSelect.innerHTML = '<option value="">-- Select level first --</option>';
            }

            this.updateConfiguration();
        });

        attrSelect.addEventListener('change', () => this.updateConfiguration());
        operatorSelect.addEventListener('change', () => this.updateConfiguration());
        resourcePropInput.addEventListener('input', () => this.updateConfiguration());
        wildcardInput.addEventListener('input', () => this.updateConfiguration());
    }

    updateValuesInput() {
        const operator = document.getElementById('matchOperator').value;
        const valuesInput = document.getElementById('matchValues');

        if (operator === 'in' || operator === 'notIn') {
            valuesInput.placeholder = 'e.g., Draft, Pending, Submitted';
        } else {
            valuesInput.placeholder = 'e.g., Draft';
        }
    }

    renderValueRangeForm() {
        const html = `
            <div class="card">
                <div class="card-header">
                    <h6 class="mb-0">Value Range Configuration</h6>
                </div>
                <div class="card-body">
                    <p class="text-muted">Conditional threshold checking: If resource property exceeds threshold, then attribute must meet minimum value</p>

                    <div class="form-group mb-3">
                        <label class="form-label">Resource Property</label>
                        <input type="text" class="form-control" id="rangeResourceProperty"
                               placeholder="e.g., Amount, TotalValue" />
                        <small class="form-text text-muted">The property to check (e.g., claim amount, invoice total)</small>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Threshold Value</label>
                        <input type="number" class="form-control" id="rangeThreshold"
                               placeholder="e.g., 50000" />
                        <small class="form-text text-muted">If resource property exceeds this value, additional checks apply</small>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Required Attribute Level</label>
                        <select class="form-control" id="rangeAttributeLevel">
                            <option value="">-- Select Level --</option>
                            <option value="user">User Attribute</option>
                            <option value="group">Group Attribute</option>
                            <option value="role">Role Attribute</option>
                        </select>
                        <small class="form-text text-muted">Which level has the required attribute</small>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Required Attribute Name</label>
                        <select class="form-control" id="rangeAttributeName" disabled>
                            <option value="">-- Select level first --</option>
                        </select>
                        <small class="form-text text-muted">The attribute that must meet the minimum value when threshold is exceeded</small>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Minimum Value</label>
                        <input type="number" class="form-control" id="rangeMinValue"
                               placeholder="e.g., 2" />
                        <small class="form-text text-muted">The minimum value the attribute must have when threshold is exceeded</small>
                    </div>

                    <div class="alert alert-info mt-3">
                        <strong>Example:</strong> If Amount > 50000, then ManagementLevel must be >= 2
                    </div>
                </div>
            </div>
        `;

        this.formContainer.innerHTML = html;
        this.attachValueRangeListeners();
        this.populateFormFromConfig();
    }

    attachValueRangeListeners() {
        const resourcePropInput = document.getElementById('rangeResourceProperty');
        const thresholdInput = document.getElementById('rangeThreshold');
        const attributeLevelSelect = document.getElementById('rangeAttributeLevel');
        const attributeNameSelect = document.getElementById('rangeAttributeName');
        const minValueInput = document.getElementById('rangeMinValue');

        resourcePropInput.addEventListener('input', () => this.updateConfiguration());
        thresholdInput.addEventListener('input', () => this.updateConfiguration());
        minValueInput.addEventListener('input', () => this.updateConfiguration());

        // Handle attribute level selection
        attributeLevelSelect.addEventListener('change', () => {
            const level = attributeLevelSelect.value;
            if (level && this.attributeSchemas[level]) {
                attributeNameSelect.disabled = false;
                attributeNameSelect.innerHTML = '<option value="">-- Select Attribute --</option>';
                this.attributeSchemas[level].forEach(schema => {
                    const option = document.createElement('option');
                    option.value = schema.attributeName;
                    option.textContent = schema.attributeDisplayName || schema.attributeName;
                    attributeNameSelect.appendChild(option);
                });
            } else {
                attributeNameSelect.disabled = true;
                attributeNameSelect.innerHTML = '<option value="">-- Select level first --</option>';
            }
            this.updateConfiguration();
        });

        attributeNameSelect.addEventListener('change', () => this.updateConfiguration());
    }

    renderTimeRestrictionForm() {
        const html = `
            <div class="card">
                <div class="card-header">
                    <h6 class="mb-0">Time Restriction Configuration</h6>
                </div>
                <div class="card-body">
                    <p class="text-muted">Restrict access based on resource classification and time windows</p>

                    <div class="form-group mb-3">
                        <label class="form-label">Resource Classification</label>
                        <input type="text" class="form-control" id="timeResourceClassification"
                               placeholder="e.g., Confidential, Internal" />
                        <small class="form-text text-muted">Only apply time restrictions to resources with this classification</small>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Allowed Hours (24-hour format)</label>
                        <div class="row">
                            <div class="col-md-6">
                                <label class="form-label small">Start Hour</label>
                                <input type="number" class="form-control" id="timeStartHour"
                                       min="0" max="23" value="8" placeholder="e.g., 8 for 8:00 AM" />
                                <small class="form-text text-muted">0-23 (e.g., 8 = 8:00 AM, 17 = 5:00 PM)</small>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label small">End Hour</label>
                                <input type="number" class="form-control" id="timeEndHour"
                                       min="0" max="23" value="18" placeholder="e.g., 18 for 6:00 PM" />
                                <small class="form-text text-muted">0-23 (e.g., 8 = 8:00 AM, 17 = 5:00 PM)</small>
                            </div>
                        </div>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Timezone</label>
                        <select class="form-control" id="timeTimezone">
                            <option value="UTC">UTC</option>
                            <option value="America/New_York">Eastern Time</option>
                            <option value="America/Chicago">Central Time</option>
                            <option value="America/Denver">Mountain Time</option>
                            <option value="America/Los_Angeles">Pacific Time</option>
                            <option value="Europe/London">London</option>
                            <option value="Europe/Paris">Paris</option>
                            <option value="Asia/Tokyo">Tokyo</option>
                        </select>
                        <small class="form-text text-muted">Timezone for evaluating time restrictions</small>
                    </div>

                    <div class="alert alert-info mt-3">
                        <strong>Example:</strong> Access to Confidential resources is only allowed from 8:00 to 18:00 UTC
                    </div>
                </div>
            </div>
        `;

        this.formContainer.innerHTML = html;
        this.attachTimeRestrictionListeners();
        this.populateFormFromConfig();
    }

    attachTimeRestrictionListeners() {
        const classification = document.getElementById('timeResourceClassification');
        const startHour = document.getElementById('timeStartHour');
        const endHour = document.getElementById('timeEndHour');
        const timezone = document.getElementById('timeTimezone');

        classification.addEventListener('input', () => this.updateConfiguration());
        startHour.addEventListener('input', () => this.updateConfiguration());
        endHour.addEventListener('input', () => this.updateConfiguration());
        timezone.addEventListener('change', () => this.updateConfiguration());
    }

    renderAttributeValueForm() {
        const html = `
            <div class="card">
                <div class="card-header">
                    <h6 class="mb-0">Attribute Value Configuration</h6>
                </div>
                <div class="card-body">
                    <p class="text-muted">Check if an attribute value matches expected value(s)</p>

                    <div class="row mb-3">
                        <div class="col-md-6">
                            <label class="form-label">Attribute Level</label>
                            <select class="form-control" id="attrLevel" required>
                                <option value="">-- Select Level --</option>
                                <option value="group">Group Attribute</option>
                                <option value="user">User Attribute</option>
                                <option value="role">Role Attribute</option>
                                <option value="resource">Resource Attribute</option>
                            </select>
                            <small class="form-text text-muted">
                                Group/User/Role attributes from database, or Resource properties from entity data
                            </small>
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Attribute Name</label>
                            <select class="form-control" id="attrName" disabled required>
                                <option value="">-- Select level first --</option>
                            </select>
                            <small class="form-text text-muted">
                                Select from attributes defined in Attribute Schemas
                            </small>
                        </div>
                    </div>

                    <div class="form-group mb-3">
                        <label class="form-label">Operator</label>
                        <select class="form-control" id="attrOperator">
                            <option value="equals">Equals</option>
                            <option value="notEquals">Not Equals</option>
                            <option value="greaterThan">Greater Than</option>
                            <option value="lessThan">Less Than</option>
                            <option value="contains">Contains</option>
                            <option value="in">In List</option>
                            <option value="notIn">Not In List</option>
                            <option value="exists">Exists (has value)</option>
                        </select>
                    </div>

                    <div class="form-group mb-3" id="attrValueContainer">
                        <label class="form-label">Expected Value</label>
                        <input type="text" class="form-control" id="attrValue"
                               placeholder="e.g., true, 1000, Admin" />
                    </div>
                </div>
            </div>
        `;

        this.formContainer.innerHTML = html;
        this.attachAttributeValueListeners();
        this.populateFormFromConfig();
    }

    attachAttributeValueListeners() {
        const levelSelect = document.getElementById('attrLevel');
        const attrSelect = document.getElementById('attrName');
        const operatorSelect = document.getElementById('attrOperator');
        const valueInput = document.getElementById('attrValue');

        levelSelect.addEventListener('change', () => {
            const level = levelSelect.value;
            attrSelect.disabled = !level;

            if (level) {
                const schemas = this.attributeSchemas[level] || [];
                attrSelect.innerHTML = '<option value="">-- Select attribute --</option>' +
                    schemas.map(s => `<option value="${s.attributeName}" data-type="${s.dataType}" data-allowed-values='${JSON.stringify(s.allowedValues || [])}'>${s.attributeDisplayName}</option>`).join('');
            } else {
                attrSelect.innerHTML = '<option value="">-- Select level first --</option>';
            }

            this.updateConfiguration();
        });

        attrSelect.addEventListener('change', () => {
            // Update value input based on selected attribute's allowed values
            this.updateAttributeValueInput();
            this.updateConfiguration();
        });

        operatorSelect.addEventListener('change', () => {
            // Hide value input for "exists" operator, update for "in"/"notIn"
            const valueContainer = document.getElementById('attrValueContainer');
            valueContainer.style.display = operatorSelect.value === 'exists' ? 'none' : 'block';
            // Re-render value input when operator changes to/from in/notIn
            this.updateAttributeValueInput();
            this.updateConfiguration();
        });

        if (valueInput) {
            valueInput.addEventListener('input', () => this.updateConfiguration());
        } else {
            // For dropdowns
            const valueSelect = document.getElementById('attrValueSelect');
            if (valueSelect) {
                valueSelect.addEventListener('change', () => this.updateConfiguration());
            }
        }
    }

    updateAttributeValueInput() {
        const attrSelect = document.getElementById('attrName');
        const operatorSelect = document.getElementById('attrOperator');
        const selectedOption = attrSelect.options[attrSelect.selectedIndex];
        const valueContainer = document.getElementById('attrValueContainer');
        const operator = operatorSelect ? operatorSelect.value : '';
        const isMultiValue = operator === 'in' || operator === 'notIn';

        if (!selectedOption || !selectedOption.value) {
            valueContainer.innerHTML = `
                <label class="form-label">Expected Value${isMultiValue ? 's' : ''}</label>
                <input type="text" class="form-control" id="attrValue"
                       placeholder="e.g., true, 1000, Admin" />
            `;
            return;
        }

        const allowedValues = JSON.parse(selectedOption.dataset.allowedValues || '[]');
        const dataType = selectedOption.dataset.type;

        if (allowedValues && allowedValues.length > 0) {
            // Use dropdown for allowed values
            const options = allowedValues.map(val => {
                const displayValue = typeof val === 'boolean' ? (val ? 'True' : 'False') : val;
                return `<option value="${val}">${displayValue}</option>`;
            }).join('');

            if (isMultiValue) {
                // Multi-select for "in" and "notIn" operators
                valueContainer.innerHTML = `
                    <label class="form-label">Expected Values (select multiple)</label>
                    <select class="form-control" id="attrValueSelect" multiple size="5">
                        ${options}
                    </select>
                    <small class="form-text text-muted">Hold Ctrl/Cmd to select multiple values</small>
                `;
            } else {
                // Single select for other operators
                valueContainer.innerHTML = `
                    <label class="form-label">Expected Value</label>
                    <select class="form-control" id="attrValueSelect">
                        <option value="">-- Select value --</option>
                        ${options}
                    </select>
                    <small class="form-text text-muted">Valid values for this attribute</small>
                `;
            }

            // Attach event listener to new dropdown
            document.getElementById('attrValueSelect').addEventListener('change', () => this.updateConfiguration());
        } else if (dataType === 'Boolean') {
            // Boolean dropdown (doesn't make sense for multi-value)
            valueContainer.innerHTML = `
                <label class="form-label">Expected Value</label>
                <select class="form-control" id="attrValueSelect">
                    <option value="">-- Select value --</option>
                    <option value="true">True</option>
                    <option value="false">False</option>
                </select>
            `;

            // Attach event listener to new dropdown
            document.getElementById('attrValueSelect').addEventListener('change', () => this.updateConfiguration());
        } else {
            // Free-text input for other types
            if (isMultiValue) {
                const placeholder = dataType === 'Number' ? 'e.g., 1,2,3' :
                                   dataType === 'String' ? 'e.g., Admin,Manager,User' :
                                   'e.g., value1,value2,value3';
                valueContainer.innerHTML = `
                    <label class="form-label">Expected Values</label>
                    <input type="text" class="form-control" id="attrValue"
                           placeholder="${placeholder}" />
                    <small class="form-text text-muted">Enter comma-separated values</small>
                `;
            } else {
                const placeholder = dataType === 'Number' ? 'e.g., 1000' :
                                   dataType === 'String' ? 'e.g., Admin' :
                                   'e.g., true, 1000, Admin';

                valueContainer.innerHTML = `
                    <label class="form-label">Expected Value</label>
                    <input type="text" class="form-control" id="attrValue"
                           placeholder="${placeholder}" />
                `;
            }

            // Attach event listener to new input
            document.getElementById('attrValue').addEventListener('input', () => this.updateConfiguration());
        }
    }

    updateConfiguration() {
        const ruleType = this.ruleTypeSelect.value;
        let config = {};

        switch (ruleType) {
            case 'AttributeComparison':
                config = this.buildAttributeComparisonConfig();
                break;
            case 'PropertyMatch':
                config = this.buildPropertyMatchConfig();
                break;
            case 'ValueRange':
                config = this.buildValueRangeConfig();
                break;
            case 'TimeRestriction':
                config = this.buildTimeRestrictionConfig();
                break;
            case 'AttributeValue':
                config = this.buildAttributeValueConfig();
                break;
        }

        this.currentConfig = config;
        this.configTextarea.value = JSON.stringify(config, null, 2);
    }

    buildAttributeComparisonConfig() {
        const level = document.getElementById('leftAttributeLevel')?.value;
        const attr = document.getElementById('leftAttributeName')?.value;
        const operator = document.getElementById('comparisonOperator')?.value;
        const rightProp = document.getElementById('rightProperty')?.value;

        if (!level || !attr || !rightProp) return {};

        return {
            leftAttribute: `${level}.${attr}`,
            operator: operator,
            rightProperty: rightProp
        };
    }

    buildPropertyMatchConfig() {
        const level = document.getElementById('pmAttributeLevel')?.value;
        const attr = document.getElementById('pmAttributeName')?.value;
        const operator = document.getElementById('pmOperator')?.value;
        const resourceProp = document.getElementById('pmResourceProperty')?.value;
        const wildcard = document.getElementById('pmAllowWildcard')?.value;

        if (!level || !attr || !resourceProp) return {};

        const config = {
            userAttribute: attr,
            operator: operator,
            resourceProperty: resourceProp
        };

        if (wildcard && wildcard.trim()) {
            config.allowWildcard = wildcard.trim();
        }

        return config;
    }

    buildValueRangeConfig() {
        const resourceProp = document.getElementById('rangeResourceProperty')?.value;
        const threshold = document.getElementById('rangeThreshold')?.value;
        const level = document.getElementById('rangeAttributeLevel')?.value;
        const attr = document.getElementById('rangeAttributeName')?.value;
        const minValue = document.getElementById('rangeMinValue')?.value;

        if (!resourceProp || !threshold || !attr || !minValue) return {};

        return {
            resourceProperty: resourceProp,
            threshold: parseFloat(threshold),
            requiredAttribute: attr,
            minValue: parseFloat(minValue)
        };
    }

    buildTimeRestrictionConfig() {
        const classification = document.getElementById('timeResourceClassification')?.value;
        const startHour = document.getElementById('timeStartHour')?.value;
        const endHour = document.getElementById('timeEndHour')?.value;
        const timezone = document.getElementById('timeTimezone')?.value;

        if (!classification || !startHour || !endHour) return {};

        return {
            resourceClassification: classification,
            allowedHours: {
                start: parseInt(startHour),
                end: parseInt(endHour)
            },
            timezone: timezone || 'UTC'
        };
    }

    buildAttributeValueConfig() {
        const level = document.getElementById('attrLevel')?.value;
        const attr = document.getElementById('attrName')?.value;
        const operator = document.getElementById('attrOperator')?.value;

        if (!level || !attr) return {};

        const config = {
            attribute: attr,
            operator: operator
        };

        // Handle multi-value operators (in, notIn)
        if (operator === 'in' || operator === 'notIn') {
            const valueSelect = document.getElementById('attrValueSelect');
            const valueInput = document.getElementById('attrValue');

            if (valueSelect && valueSelect.multiple) {
                // Multi-select dropdown - get all selected options
                const selectedOptions = Array.from(valueSelect.selectedOptions).map(opt => opt.value);
                if (selectedOptions.length > 0) {
                    config.values = selectedOptions.map(val => this.parseValue(val));
                }
            } else if (valueInput) {
                // Comma-separated text input
                const rawValues = valueInput.value.split(',').map(v => v.trim()).filter(v => v);
                if (rawValues.length > 0) {
                    config.values = rawValues.map(val => this.parseValue(val));
                }
            }
        } else if (operator !== 'exists') {
            // Single value operators
            const valueSelect = document.getElementById('attrValueSelect');
            const valueInput = document.getElementById('attrValue');
            const value = valueSelect ? valueSelect.value : (valueInput ? valueInput.value : null);

            if (value) {
                config.value = this.parseValue(value);
            }
        }

        return config;
    }

    parseValue(value) {
        // Try to parse as number or boolean
        if (value === 'true' || value === 'false') {
            return value === 'true';
        } else if (!isNaN(value) && value !== '') {
            return parseFloat(value);
        } else {
            return value;
        }
    }

    populateFormFromConfig() {
        if (!this.currentConfig || Object.keys(this.currentConfig).length === 0) return;

        const ruleType = this.ruleTypeSelect.value;

        switch (ruleType) {
            case 'AttributeComparison':
                this.populateAttributeComparisonForm();
                break;
            case 'PropertyMatch':
                this.populatePropertyMatchForm();
                break;
            case 'ValueRange':
                this.populateValueRangeForm();
                break;
            case 'TimeRestriction':
                this.populateTimeRestrictionForm();
                break;
            case 'AttributeValue':
                this.populateAttributeValueForm();
                break;
        }
    }

    populateAttributeComparisonForm() {
        const config = this.currentConfig;
        if (config.leftAttribute) {
            const [level, attr] = config.leftAttribute.split('.');
            const levelSelect = document.getElementById('leftAttributeLevel');
            levelSelect.value = level;
            levelSelect.dispatchEvent(new Event('change'));

            setTimeout(() => {
                document.getElementById('leftAttributeName').value = attr;
            }, 100);
        }
        if (config.operator) {
            document.getElementById('comparisonOperator').value = config.operator;
        }
        if (config.rightProperty) {
            document.getElementById('rightProperty').value = config.rightProperty;
        }

        // Ensure the configuration textarea reflects the populated values
        setTimeout(() => {
            this.updateConfiguration();
        }, 150);
    }

    populatePropertyMatchForm() {
        const config = this.currentConfig;

        // Determine attribute level from userAttribute value
        if (config.userAttribute) {
            // Try to find which level has this attribute
            let foundLevel = null;
            for (const [level, schemas] of Object.entries(this.attributeSchemas)) {
                if (schemas.some(s => s.attributeName === config.userAttribute)) {
                    foundLevel = level;
                    break;
                }
            }

            if (foundLevel) {
                const levelSelect = document.getElementById('pmAttributeLevel');
                levelSelect.value = foundLevel;
                levelSelect.dispatchEvent(new Event('change'));

                // Set attribute name after schema loads
                setTimeout(() => {
                    document.getElementById('pmAttributeName').value = config.userAttribute;
                }, 100);
            }
        }

        if (config.operator) {
            document.getElementById('pmOperator').value = config.operator;
        }

        if (config.resourceProperty) {
            document.getElementById('pmResourceProperty').value = config.resourceProperty;
        }

        if (config.allowWildcard) {
            document.getElementById('pmAllowWildcard').value = config.allowWildcard;
        }

        // Ensure the configuration textarea reflects the populated values
        setTimeout(() => {
            this.updateConfiguration();
        }, 150);
    }

    populateValueRangeForm() {
        const config = this.currentConfig;

        if (config.resourceProperty) {
            document.getElementById('rangeResourceProperty').value = config.resourceProperty;
        }

        if (config.threshold !== undefined) {
            document.getElementById('rangeThreshold').value = config.threshold;
        }

        if (config.minValue !== undefined) {
            document.getElementById('rangeMinValue').value = config.minValue;
        }

        // Find which level has the required attribute
        if (config.requiredAttribute) {
            let foundLevel = null;
            for (const [level, schemas] of Object.entries(this.attributeSchemas)) {
                if (schemas.some(s => s.attributeName === config.requiredAttribute)) {
                    foundLevel = level;
                    break;
                }
            }

            if (foundLevel) {
                const levelSelect = document.getElementById('rangeAttributeLevel');
                levelSelect.value = foundLevel;
                levelSelect.dispatchEvent(new Event('change'));

                // Set attribute name after schema loads
                setTimeout(() => {
                    document.getElementById('rangeAttributeName').value = config.requiredAttribute;
                }, 100);
            }
        }

        // Ensure the configuration textarea reflects the populated values
        setTimeout(() => {
            this.updateConfiguration();
        }, 150);
    }

    populateTimeRestrictionForm() {
        const config = this.currentConfig;

        if (config.resourceClassification) {
            document.getElementById('timeResourceClassification').value = config.resourceClassification;
        }

        if (config.allowedHours) {
            if (config.allowedHours.start !== undefined) {
                document.getElementById('timeStartHour').value = config.allowedHours.start;
            }
            if (config.allowedHours.end !== undefined) {
                document.getElementById('timeEndHour').value = config.allowedHours.end;
            }
        }

        if (config.timezone) {
            document.getElementById('timeTimezone').value = config.timezone;
        }

        // Ensure the configuration textarea reflects the populated values
        setTimeout(() => {
            this.updateConfiguration();
        }, 150);
    }

    populateAttributeValueForm() {
        const config = this.currentConfig;

        // Set operator first
        if (config.operator) {
            document.getElementById('attrOperator').value = config.operator;
            if (config.operator === 'exists') {
                document.getElementById('attrValueContainer').style.display = 'none';
            }
        }

        // Find which level has this attribute
        if (config.attribute) {
            let foundLevel = null;
            for (const [level, schemas] of Object.entries(this.attributeSchemas)) {
                if (schemas.some(s => s.attributeName === config.attribute)) {
                    foundLevel = level;
                    break;
                }
            }

            if (foundLevel) {
                const levelSelect = document.getElementById('attrLevel');
                levelSelect.value = foundLevel;
                levelSelect.dispatchEvent(new Event('change'));

                setTimeout(() => {
                    const attrSelect = document.getElementById('attrName');
                    attrSelect.value = config.attribute;
                    attrSelect.dispatchEvent(new Event('change'));

                    // After the value input is created, populate it
                    setTimeout(() => {
                        // Handle multi-value operators (in, notIn)
                        if (config.values !== undefined && Array.isArray(config.values)) {
                            const valueSelect = document.getElementById('attrValueSelect');
                            const valueInput = document.getElementById('attrValue');

                            if (valueSelect && valueSelect.multiple) {
                                // Multi-select dropdown - select multiple options
                                Array.from(valueSelect.options).forEach(option => {
                                    option.selected = config.values.some(val =>
                                        option.value == val || option.value === String(val)
                                    );
                                });
                            } else if (valueInput) {
                                // Comma-separated text input
                                valueInput.value = config.values.join(', ');
                            }
                        } else if (config.value !== undefined) {
                            // Single value operators
                            const valueSelect = document.getElementById('attrValueSelect');
                            const valueInput = document.getElementById('attrValue');

                            if (valueSelect) {
                                valueSelect.value = config.value;
                            } else if (valueInput) {
                                valueInput.value = config.value;
                            }
                        }
                    }, 100);
                }, 100);
            }
        }

        // Ensure the configuration textarea reflects the populated values
        setTimeout(() => {
            this.updateConfiguration();
        }, 250);
    }
}
