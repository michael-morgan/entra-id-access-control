// Dynamic Attribute Form Builder
// Fetches AttributeSchemas from API and builds form-based UI for attribute management

class AttributeFormBuilder {
    constructor(workstreamId, attributeLevel, containerSelector, hiddenInputSelector) {
        this.workstreamId = workstreamId;
        this.attributeLevel = attributeLevel;
        this.container = document.querySelector(containerSelector);
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

        const formHtml = this.schemas.map(schema => this.renderField(schema)).join('');
        this.container.innerHTML = `
            <div class="attribute-form-fields">
                ${formHtml}
            </div>
        `;
    }

    renderField(schema) {
        const value = this.attributes[schema.attributeName] || schema.defaultValue || '';
        const required = schema.isRequired ? 'required' : '';
        const requiredBadge = schema.isRequired ? '<span class="badge bg-danger ms-2">Required</span>' : '';

        let inputHtml = '';

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
        const inputs = this.container.querySelectorAll('[data-attribute-name]');
        inputs.forEach(input => {
            input.addEventListener('change', () => this.updateAttributes());
            input.addEventListener('input', () => this.updateAttributes());
        });

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
        const inputs = this.container.querySelectorAll('[data-attribute-name]');
        const newAttributes = {};

        inputs.forEach(input => {
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
        const inputs = this.container.querySelectorAll('[data-attribute-name]');

        inputs.forEach(input => {
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
