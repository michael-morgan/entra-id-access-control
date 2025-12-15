// Dynamic Attribute Form Builder
// Fetches AttributeSchemas from API and builds form-based UI for attribute management

class AttributeFormBuilder {
    constructor(workstreamId, attributeLevel, containerSelector, hiddenInputSelector, optionalContainerSelector = null) {
        this.workstreamId = workstreamId;
        this.attributeLevel = attributeLevel;
        this.container = document.querySelector(containerSelector);
        this.optionalContainer = optionalContainerSelector ? document.querySelector(optionalContainerSelector) : null;
        this.hiddenInput = document.querySelector(hiddenInputSelector);
        this.schemas = [];
        this.attributes = {};
    }

    async init() {
        await this.fetchSchemas();
        this.loadExistingAttributes();
        this.render();
        this.attachEventListeners();
    }

    async fetchSchemas() {
        try {
            const response = await fetch(`/api/attribute-schemas?workstreamId=${this.workstreamId}&attributeLevel=${this.attributeLevel}`);
            if (!response.ok) {
                throw new Error('Failed to fetch attribute schemas');
            }
            this.schemas = await response.json();
        } catch (error) {
            console.error('Error fetching schemas:', error);
            this.showError('Unable to load attribute schemas. Please refresh the page.');
        }
    }

    loadExistingAttributes() {
        try {
            const jsonValue = this.hiddenInput.value.trim();
            if (jsonValue) {
                this.attributes = JSON.parse(jsonValue);
            }
        } catch (error) {
            console.error('Error parsing existing attributes:', error);
            this.attributes = {};
        }
    }

    render() {
        if (this.schemas.length === 0) {
            this.container.innerHTML = `
                <div class="alert alert-warning">
                    No attribute schemas configured for workstream <strong>${this.workstreamId}</strong> and level <strong>${this.attributeLevel}</strong>.
                </div>
            `;
            return;
        }

        // Separate required and optional schemas
        const requiredSchemas = this.schemas.filter(s => s.isRequired);
        const optionalSchemas = this.schemas.filter(s => !s.isRequired);

        // Render required fields in main container
        const requiredHtml = requiredSchemas.map(schema => this.renderField(schema)).join('');
        if (requiredSchemas.length > 0) {
            this.container.innerHTML = `
                <div class="attribute-form-fields">
                    ${requiredHtml}
                </div>
            `;
        } else {
            this.container.innerHTML = `
                <div class="alert alert-info">
                    No required attributes configured.
                </div>
            `;
        }

        // Render optional fields in optional container if provided
        if (this.optionalContainer && optionalSchemas.length > 0) {
            const optionalHtml = optionalSchemas.map(schema => this.renderField(schema)).join('');
            this.optionalContainer.innerHTML = `
                <div class="attribute-form-fields">
                    ${optionalHtml}
                </div>
            `;
        } else if (this.optionalContainer) {
            this.optionalContainer.innerHTML = `
                <p class="text-muted small">Optional attributes will appear here based on the AttributeSchemas configured for this workstream and attribute level.</p>
            `;
        }
    }

    renderField(schema) {
        const value = this.attributes[schema.attributeName] || schema.defaultValue || '';
        const required = schema.isRequired ? 'required' : '';
        const requiredBadge = schema.isRequired ? '<span class="badge bg-danger ms-2">Required</span>' : '';

        let inputHtml = '';

        // Check if validationRules contains allowedValues
        let allowedValues = null;
        if (schema.validationRules) {
            try {
                const rules = typeof schema.validationRules === 'string'
                    ? JSON.parse(schema.validationRules)
                    : schema.validationRules;
                allowedValues = rules.allowedValues;
            } catch (e) {
                console.warn('Failed to parse validation rules for', schema.attributeName, e);
            }
        }

        // If allowedValues exist, render as dropdown regardless of data type
        if (allowedValues && Array.isArray(allowedValues) && allowedValues.length > 0) {
            const options = allowedValues.map(val => {
                const selected = val === value ? 'selected' : '';
                return `<option value="${this.escapeHtml(val)}" ${selected}>${this.escapeHtml(val)}</option>`;
            }).join('');

            inputHtml = `
                <select class="form-select"
                    id="attr_${schema.attributeName}"
                    data-attribute-name="${schema.attributeName}"
                    ${required}>
                    <option value="">-- Select a value --</option>
                    ${options}
                </select>
            `;
        } else {
            // Render based on data type
            switch (schema.dataType.toLowerCase()) {
                case 'string':
                    inputHtml = `<input type="text"
                        class="form-control"
                        id="attr_${schema.attributeName}"
                        data-attribute-name="${schema.attributeName}"
                        value="${this.escapeHtml(value)}"
                        ${required} />`;
                    break;

                case 'number':
                    inputHtml = `<input type="number"
                        class="form-control"
                        id="attr_${schema.attributeName}"
                        data-attribute-name="${schema.attributeName}"
                        value="${value}"
                        ${required} />`;
                    break;

                case 'boolean':
                    const checked = value === true || value === 'true' ? 'checked' : '';
                    inputHtml = `
                        <div class="form-check form-switch">
                            <input type="checkbox"
                                class="form-check-input"
                                id="attr_${schema.attributeName}"
                                data-attribute-name="${schema.attributeName}"
                                ${checked}
                                ${required} />
                            <label class="form-check-label" for="attr_${schema.attributeName}">
                                ${schema.isRequired ? 'This attribute is required' : 'Enable this attribute'}
                            </label>
                        </div>
                    `;
                    break;

                case 'date':
                    const dateValue = value ? new Date(value).toISOString().split('T')[0] : '';
                    inputHtml = `<input type="date"
                        class="form-control"
                        id="attr_${schema.attributeName}"
                        data-attribute-name="${schema.attributeName}"
                        value="${dateValue}"
                        ${required} />`;
                    break;

                default:
                    inputHtml = `<textarea
                        class="form-control"
                        id="attr_${schema.attributeName}"
                        data-attribute-name="${schema.attributeName}"
                        rows="3"
                        ${required}>${this.escapeHtml(value)}</textarea>`;
            }
        }

        return `
            <div class="mb-3">
                <label for="attr_${schema.attributeName}" class="form-label">
                    ${schema.attributeDisplayName}
                    ${requiredBadge}
                </label>
                ${inputHtml}
                ${schema.description ? `<div class="form-text">${schema.description}</div>` : ''}
            </div>
        `;
    }

    attachEventListeners() {
        // Attach listeners to required fields
        const inputs = this.container.querySelectorAll('[data-attribute-name]');
        inputs.forEach(input => {
            input.addEventListener('change', () => this.updateAttributes());
            input.addEventListener('input', () => this.updateAttributes());
        });

        // Attach listeners to optional fields if container exists
        if (this.optionalContainer) {
            const optionalInputs = this.optionalContainer.querySelectorAll('[data-attribute-name]');
            optionalInputs.forEach(input => {
                input.addEventListener('change', () => this.updateAttributes());
                input.addEventListener('input', () => this.updateAttributes());
            });
        }

        const form = this.container.closest('form');
        if (form) {
            form.addEventListener('submit', (e) => {
                if (!this.validate()) {
                    e.preventDefault();
                }
            });
        }
    }

    updateAttributes() {
        // Collect all inputs from both containers
        const allInputs = [
            ...this.container.querySelectorAll('[data-attribute-name]'),
            ...(this.optionalContainer ? this.optionalContainer.querySelectorAll('[data-attribute-name]') : [])
        ];

        const newAttributes = {};

        allInputs.forEach(input => {
            const attrName = input.getAttribute('data-attribute-name');
            const schema = this.schemas.find(s => s.attributeName === attrName);

            if (!schema) return;

            let value;

            if (input.type === 'checkbox') {
                value = input.checked;
            } else if (schema.dataType.toLowerCase() === 'number') {
                value = input.value ? parseFloat(input.value) : null;
            } else {
                value = input.value;
            }

            if (value !== null && value !== '') {
                newAttributes[attrName] = value;
            }
        });

        this.attributes = newAttributes;
        this.hiddenInput.value = JSON.stringify(newAttributes, null, 2);
    }

    validate() {
        let isValid = true;

        // Collect all inputs from both containers
        const allInputs = [
            ...this.container.querySelectorAll('[data-attribute-name]'),
            ...(this.optionalContainer ? this.optionalContainer.querySelectorAll('[data-attribute-name]') : [])
        ];

        allInputs.forEach(input => {
            const attrName = input.getAttribute('data-attribute-name');
            const schema = this.schemas.find(s => s.attributeName === attrName);

            if (!schema) return;

            if (schema.isRequired && (input.value === '' || (input.type === 'checkbox' && !input.checked && schema.dataType.toLowerCase() === 'boolean'))) {
                isValid = false;
                input.classList.add('is-invalid');
            } else {
                input.classList.remove('is-invalid');
            }
        });

        if (!isValid) {
            this.showError('Please fill in all required fields.');
        }

        return isValid;
    }

    showError(message) {
        let alertDiv = this.container.querySelector('.alert-danger');
        if (!alertDiv) {
            alertDiv = document.createElement('div');
            alertDiv.className = 'alert alert-danger alert-dismissible fade show';
            alertDiv.innerHTML = `
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
                <span class="error-message"></span>
            `;
            this.container.insertBefore(alertDiv, this.container.firstChild);
        }
        alertDiv.querySelector('.error-message').textContent = message;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

window.AttributeFormBuilder = AttributeFormBuilder;
