import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HostelService } from './hostel.service';

describe('HostelService update and delete', () => {
  let service: HostelService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(HostelService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('PUTs a hostel update', () => {
    service.updateHostel('h1', 'Renamed', 'Mumbai').subscribe();

    const request = http.expectOne('/api/hostels/h1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Renamed', location: 'Mumbai' });
    request.flush({});
  });

  it('DELETEs a hostel', () => {
    service.deleteHostel('h1').subscribe();

    const request = http.expectOne('/api/hostels/h1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  it('PUTs a room update under its hostel', () => {
    service.updateRoom('h1', 'r1', 'Room One A').subscribe();

    const request = http.expectOne('/api/hostels/h1/rooms/r1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Room One A' });
    request.flush({});
  });

  it('DELETEs a room under its hostel', () => {
    service.deleteRoom('h1', 'r1').subscribe();

    const request = http.expectOne('/api/hostels/h1/rooms/r1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  it('PUTs a cot update under its room', () => {
    service.updateCot('r1', 'c1', 'Cot A1').subscribe();

    const request = http.expectOne('/api/rooms/r1/cots/c1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ label: 'Cot A1' });
    request.flush({});
  });

  it('DELETEs a cot under its room', () => {
    service.deleteCot('r1', 'c1').subscribe();

    const request = http.expectOne('/api/rooms/r1/cots/c1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
