import { HttpErrorResponse } from '@angular/common/http';
import { apiErrorMessage } from './api-error';

describe('apiErrorMessage', () => {
  it('uses the server text from a 409 body', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: 'This room still has cots. Delete them first.',
    });

    expect(apiErrorMessage(error, 'Failed.')).toBe('This room still has cots. Delete them first.');
  });

  it('uses the server text from a 400 body', () => {
    const error = new HttpErrorResponse({ status: 400, error: 'Amount must be positive.' });

    expect(apiErrorMessage(error, 'Failed.')).toBe('Amount must be positive.');
  });

  it('falls back when the failure carries no body text', () => {
    const error = new HttpErrorResponse({ status: 500, error: null });

    expect(apiErrorMessage(error, 'Failed to delete hostel.')).toBe('Failed to delete hostel.');
  });

  it('falls back for a blank body', () => {
    const error = new HttpErrorResponse({ status: 409, error: '   ' });

    expect(apiErrorMessage(error, 'Failed.')).toBe('Failed.');
  });

  it('reads ProblemDetails detail when the body is an object', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { title: 'One or more validation errors occurred.', detail: 'Name is required.' },
    });

    expect(apiErrorMessage(error, 'Failed.')).toBe('Name is required.');
  });

  it('falls back for a non-HTTP error', () => {
    expect(apiErrorMessage(new Error('boom'), 'Failed.')).toBe('Failed.');
  });
});
