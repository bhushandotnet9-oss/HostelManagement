import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { NavShellComponent } from './nav-shell.component';

describe('NavShellComponent', () => {
  let fixture: ComponentFixture<NavShellComponent>;
  let auth: AuthService;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NavShellComponent],
      imports: [RouterModule.forRoot([])],
    }).compileComponents();

    fixture = TestBed.createComponent(NavShellComponent);
    auth = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('logs out and redirects to /login', () => {
    const logoutSpy = jest.spyOn(auth, 'logout');
    const navigateSpy = jest.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    fixture.componentInstance.logout();

    expect(logoutSpy).toHaveBeenCalled();
    expect(navigateSpy).toHaveBeenCalledWith('/login');
  });
});
