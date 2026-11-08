import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { ToastService } from '../shared/toast.service';
import { Cot, Hostel, Room } from './hostel.service';
import { HostelsComponent } from './hostels.component';

const hostel: Hostel = { id: 'h1', name: 'Hostel One', location: 'Pune' };
const room: Room = { id: 'r1', hostelId: 'h1', name: 'Room One' };
const cot: Cot = { id: 'c1', roomId: 'r1', label: 'Cot A', status: 'Vacant' };

describe('HostelsComponent edit and delete', () => {
  let fixture: ComponentFixture<HostelsComponent>;
  let component: HostelsComponent;
  let http: HttpTestingController;
  let toast: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [HostelsComponent],
      imports: [FormsModule],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(HostelsComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);

    // ngOnInit issues the initial hostel list request.
    fixture.detectChanges();
    http.expectOne('/api/hostels').flush([hostel]);
  });

  afterEach(() => http.verify());

  it('enters edit mode prefilled with the current values', () => {
    component.startEditHostel(hostel);

    expect(component.editingHostelId()).toBe('h1');
    expect(component.editHostelName).toBe('Hostel One');
    expect(component.editHostelLocation).toBe('Pune');
  });

  it('leaves edit mode on cancel without issuing a request', () => {
    component.startEditHostel(hostel);

    component.cancelEditHostel();

    expect(component.editingHostelId()).toBeNull();
    http.expectNone('/api/hostels/h1');
  });

  it('saves a rename, leaves edit mode and reloads the list', () => {
    component.startEditHostel(hostel);
    component.editHostelName = 'Hostel One Renamed';

    component.saveHostel(hostel);

    const update = http.expectOne('/api/hostels/h1');
    expect(update.request.method).toBe('PUT');
    update.flush({ ...hostel, name: 'Hostel One Renamed' });

    http.expectOne('/api/hostels').flush([{ ...hostel, name: 'Hostel One Renamed' }]);

    expect(component.editingHostelId()).toBeNull();
    expect(component.hostels()[0].name).toBe('Hostel One Renamed');
    expect(toast.toasts()[0].kind).toBe('success');
  });

  it('does not save a rename that blanks the name', () => {
    component.startEditHostel(hostel);
    component.editHostelName = '   ';

    component.saveHostel(hostel);

    http.expectNone('/api/hostels/h1');
  });

  it('issues no request when the delete confirmation is declined', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(false);

    component.deleteHostel(hostel);

    http.expectNone('/api/hostels/h1');
  });

  it('deletes a hostel on confirmation and reloads the list', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(true);

    component.deleteHostel(hostel);

    const remove = http.expectOne('/api/hostels/h1');
    expect(remove.request.method).toBe('DELETE');
    remove.flush(null);

    http.expectOne('/api/hostels').flush([]);

    expect(component.hostels()).toEqual([]);
    expect(toast.toasts()[0].kind).toBe('success');
  });

  it('shows the server message when a hostel delete is blocked', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(true);

    component.deleteHostel(hostel);

    http.expectOne('/api/hostels/h1').flush('This hostel still has rooms. Delete them first.', {
      status: 409,
      statusText: 'Conflict',
    });

    const entry = toast.toasts()[0];
    expect(entry.kind).toBe('error');
    expect(entry.message).toBe('This hostel still has rooms. Delete them first.');
  });

  it('shows the server message when a cot delete is blocked', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(true);

    component.deleteCot(cot);

    http.expectOne('/api/rooms/r1/cots/c1').flush('A resident is assigned to this cot. Check them out first.', {
      status: 409,
      statusText: 'Conflict',
    });

    const entry = toast.toasts()[0];
    expect(entry.kind).toBe('error');
    expect(entry.message).toBe('A resident is assigned to this cot. Check them out first.');
  });

  it('saves a room rename against its parent hostel', () => {
    component.startEditRoom(room);
    component.editRoomName = 'Room One A';

    component.saveRoom(room);

    const update = http.expectOne('/api/hostels/h1/rooms/r1');
    expect(update.request.method).toBe('PUT');
    update.flush({ ...room, name: 'Room One A' });

    // saveRoom reloads the hostel's rooms, which then loads each room's cots.
    http.expectOne('/api/hostels/h1/rooms').flush([room]);
    http.expectOne('/api/rooms/r1/cots').flush([cot]);

    expect(component.editingRoomId()).toBeNull();
  });

  it('falls back to a generic message when the failure has no body', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(true);

    component.deleteRoom(room);

    http
      .expectOne('/api/hostels/h1/rooms/r1')
      .error(new ProgressEvent('error'), { status: 500, statusText: 'Server Error' });

    expect(toast.toasts()[0].message).toBe('Failed to delete room.');
  });
});
