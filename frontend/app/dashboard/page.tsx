'use client';

import { useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch, clearToken, getUserInfo } from '../../lib/api';

interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

interface Notification {
  id: number;
  title: string;
  message: string;
  type: string;
  referenceId: number | null;
  isRead: boolean;
  createdAt: string;
}

function timeAgo(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime();
  const m = Math.floor(diff / 60000);
  if (m < 1) return 'just now';
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h / 24)}d ago`;
}

function notifIcon(type: string) {
  switch (type) {
    case 'assignment_published': return '📋';
    case 'submission_received':  return '📬';
    case 'grade_posted':         return '🏆';
    default:                     return '🔔';
  }
}

export default function DashboardPage() {
  const router = useRouter();
  const [user, setUser] = useState<any>(null);
  const [activeTab, setActiveTab] = useState<string>('assignments');

  const [usersList, setUsersList] = useState<any[]>([]);
  const [classesList, setClassesList] = useState<any[]>([]);
  const [newUser, setNewUser] = useState({ fullName: '', email: '', password: 'User123!', role: 'Student', classId: '' });
  const [newClass, setNewClass] = useState({ name: '', code: '' });
  const [newSubject, setNewSubject] = useState({ name: '', classId: '', teacherId: '' });

  const [assignments, setAssignments] = useState<any[]>([]);
  const [assignPage, setAssignPage] = useState(1);
  const [assignMeta, setAssignMeta] = useState({ totalCount: 0, totalPages: 1, pageSize: 9 });

  const [filters, setFilters] = useState({
    search: '', subjectId: '', isPublished: '', sortBy: 'createdAt', sortDir: 'desc'
  });
  const [showFilters, setShowFilters] = useState(false);

  const [showCreateModal, setShowCreateModal] = useState(false);
  const [newAssignment, setNewAssignment] = useState({
    title: '', description: '', deadline: '', maxMarks: 100, isPublished: true, classId: '', subjectId: ''
  });

  const [selectedAssignment, setSelectedAssignment] = useState<any>(null);
  const [submissionsList, setSubmissionsList] = useState<any[]>([]);
  const [subPage, setSubPage] = useState(1);
  const [subMeta, setSubMeta] = useState({ totalCount: 0, totalPages: 1 });
  const [subStatusFilter, setSubStatusFilter] = useState('');
  const [showSubmissionsModal, setShowSubmissionsModal] = useState(false);

  const [submitContent, setSubmitContent] = useState('');
  const [submitLink, setSubmitLink] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const [gradingSubmission, setGradingSubmission] = useState<any>(null);
  const [gradeMarks, setGradeMarks] = useState<number>(0);
  const [gradeFeedback, setGradeFeedback] = useState<string>('');
  const [gradeStatus, setGradeStatus] = useState<string>('Graded');

  const [notifOpen, setNotifOpen] = useState(false);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [notifUnread, setNotifUnread] = useState(0);
  const [notifPage, setNotifPage] = useState(1);
  const [notifMeta, setNotifMeta] = useState({ totalPages: 1 });
  const notifPanelRef = useRef<HTMLDivElement>(null);
  const notifIntervalRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    const userInfo = getUserInfo();
    if (!userInfo) { router.push('/'); return; }
    setUser(userInfo);
    if (userInfo.role === 'Admin') loadAdminData();
    loadNotifCount();

    notifIntervalRef.current = setInterval(() => loadNotifCount(), 30000);
    return () => { if (notifIntervalRef.current) clearInterval(notifIntervalRef.current); };
  }, []);

  useEffect(() => {
    if (user) loadAssignments(1);
  }, [user, filters]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (notifPanelRef.current && !notifPanelRef.current.contains(e.target as Node))
        setNotifOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const loadAdminData = async () => {
    try {
      const usersData   = await apiFetch('/users');
      const classesData = await apiFetch('/classes');
      setUsersList(Array.isArray(usersData) ? usersData : usersData.items ?? []);
      setClassesList(Array.isArray(classesData) ? classesData : classesData.items ?? []);
    } catch (err) { console.error(err); }
  };

  const buildAssignmentParams = (pg: number) => {
    const p = new URLSearchParams();
    p.set('page', String(pg));
    p.set('pageSize', '9');
    if (filters.search)       p.set('search',    filters.search);
    if (filters.subjectId)    p.set('subjectId', filters.subjectId);
    if (filters.isPublished)  p.set('isPublished', filters.isPublished);
    if (filters.sortBy)       p.set('sortBy', filters.sortBy);
    if (filters.sortDir)      p.set('sortDir', filters.sortDir);
    return p.toString();
  };

  const loadAssignments = async (pg = assignPage) => {
    try {
      const data: PagedResult<any> = await apiFetch(`/assignments?${buildAssignmentParams(pg)}`);
      setAssignments(data.items ?? []);
      setAssignPage(data.page);
      setAssignMeta({ totalCount: data.totalCount, totalPages: data.totalPages, pageSize: data.pageSize });
    } catch (err) { console.error(err); }
  };

  const loadNotifCount = async () => {
    try {
      const data = await apiFetch('/notifications?unreadOnly=true&pageSize=1');
      setNotifUnread(data.unreadCount ?? 0);
    } catch { /* silent */ }
  };

  const loadNotifications = async (pg = 1) => {
    try {
      const data = await apiFetch(`/notifications?page=${pg}&pageSize=10`);
      setNotifications(data.items ?? []);
      setNotifUnread(data.unreadCount ?? 0);
      setNotifPage(pg);
      setNotifMeta({ totalPages: data.totalPages ?? 1 });
    } catch { /* silent */ }
  };

  const handleOpenNotifPanel = () => {
    setNotifOpen(o => !o);
    if (!notifOpen) loadNotifications(1);
  };

  const handleMarkRead = async (id: number) => {
    try {
      await apiFetch(`/notifications/${id}/read`, { method: 'PUT' });
      setNotifications(ns => ns.map(n => n.id === id ? { ...n, isRead: true } : n));
      setNotifUnread(c => Math.max(0, c - 1));
    } catch { /* silent */ }
  };

  const handleMarkAllRead = async () => {
    try {
      await apiFetch('/notifications/read-all', { method: 'PUT' });
      setNotifications(ns => ns.map(n => ({ ...n, isRead: true })));
      setNotifUnread(0);
    } catch { /* silent */ }
  };

  const handleLogout = () => { clearToken(); router.push('/'); };

  const handleCreateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await apiFetch('/users', {
        method: 'POST',
        body: JSON.stringify({
          fullName: newUser.fullName, email: newUser.email,
          password: newUser.password, role: newUser.role,
          classId: newUser.classId ? parseInt(newUser.classId) : null
        })
      });
      alert('User created successfully!');
      setNewUser({ fullName: '', email: '', password: 'User123!', role: 'Student', classId: '' });
      loadAdminData();
    } catch (err: any) { alert(err.message || 'Failed to create user'); }
  };

  const handleDeleteUser = async (id: number) => {
    if (!confirm('Delete this user?')) return;
    try { await apiFetch(`/users/${id}`, { method: 'DELETE' }); loadAdminData(); }
    catch (err: any) { alert(err.message); }
  };

  const handleCreateClass = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await apiFetch('/classes', { method: 'POST', body: JSON.stringify(newClass) });
      setNewClass({ name: '', code: '' }); loadAdminData();
    } catch (err: any) { alert(err.message); }
  };

  const handleCreateSubject = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await apiFetch('/classes/subjects', {
        method: 'POST',
        body: JSON.stringify({
          name: newSubject.name,
          classId: parseInt(newSubject.classId),
          teacherId: newSubject.teacherId ? parseInt(newSubject.teacherId) : null
        })
      });
      setNewSubject({ name: '', classId: '', teacherId: '' }); loadAdminData();
    } catch (err: any) { alert(err.message); }
  };

  const handleCreateAssignment = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await apiFetch('/assignments', {
        method: 'POST',
        body: JSON.stringify({
          ...newAssignment,
          maxMarks: parseFloat(newAssignment.maxMarks as any),
          classId: parseInt(newAssignment.classId),
          subjectId: parseInt(newAssignment.subjectId),
        })
      });
      setShowCreateModal(false); loadAssignments(1);
    } catch (err: any) { alert(err.message); }
  };

  const handleDeleteAssignment = async (id: number) => {
    if (!confirm('Delete this assignment?')) return;
    try { await apiFetch(`/assignments/${id}`, { method: 'DELETE' }); loadAssignments(assignPage); }
    catch (err: any) { alert(err.message); }
  };

  const viewSubmissions = async (assignment: any, pg = 1) => {
    setSelectedAssignment(assignment);
    try {
      const params = new URLSearchParams({ page: String(pg), pageSize: '10' });
      if (subStatusFilter) params.set('statusFilter', subStatusFilter);
      const data: any = await apiFetch(`/submissions/assignment/${assignment.id}?${params}`);
      setSubmissionsList(data.items ?? []);
      setSubPage(data.page);
      setSubMeta({ totalCount: data.totalCount, totalPages: data.totalPages });
      setShowSubmissionsModal(true);
    } catch (err: any) { alert(err.message); }
  };

  const handleStudentSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedAssignment) return;
    const formData = new FormData();
    formData.append('assignmentId', selectedAssignment.id.toString());
    if (submitContent) formData.append('content', submitContent);
    if (submitLink)    formData.append('linkUrl', submitLink);
    if (selectedFile)  formData.append('file', selectedFile);
    try {
      await apiFetch('/submissions', { method: 'POST', body: formData });
      alert('Assignment submitted successfully!');
      setSelectedAssignment(null); setSubmitContent(''); setSubmitLink(''); setSelectedFile(null);
      loadAssignments(assignPage);
    } catch (err: any) { alert(err.message); }
  };

  const handleSaveGrade = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!gradingSubmission) return;
    try {
      await apiFetch(`/submissions/${gradingSubmission.id}/grade`, {
        method: 'PUT',
        body: JSON.stringify({ marks: parseFloat(gradeMarks as any), feedback: gradeFeedback, status: gradeStatus })
      });
      setGradingSubmission(null);
      viewSubmissions(selectedAssignment, subPage);
      loadAssignments(assignPage);
    } catch (err: any) { alert(err.message); }
  };

  if (!user) return null;

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>

      <header style={{
        background: 'rgba(19, 27, 46, 0.95)', borderBottom: '1px solid var(--border-color)',
        padding: '0.85rem 2rem', display: 'flex', alignItems: 'center',
        justifyContent: 'space-between', position: 'sticky', top: 0, zIndex: 50
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div style={{
            width: '40px', height: '40px', borderRadius: '10px',
            background: 'linear-gradient(135deg, #4f46e5 0%, #06b6d4 100%)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontWeight: 800, color: '#fff'
          }}>AS</div>
          <div>
            <h2 style={{ fontSize: '1.1rem', fontWeight: 700 }}>Assignment Management System</h2>
            <p style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>
              {user.role} Dashboard {user.className ? `• ${user.className}` : ''}
            </p>
          </div>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div ref={notifPanelRef} style={{ position: 'relative' }}>
            <button
              id="notif-bell"
              onClick={handleOpenNotifPanel}
              style={{
                background: 'var(--bg-input)', border: '1px solid var(--border-color)',
                borderRadius: '10px', padding: '0.45rem 0.7rem', cursor: 'pointer',
                position: 'relative', fontSize: '1.1rem', transition: 'all 0.2s'
              }}
              title="Notifications"
            >
              🔔
              {notifUnread > 0 && (
                <span className="notif-badge">{notifUnread > 99 ? '99+' : notifUnread}</span>
              )}
            </button>

            {notifOpen && (
              <div className="notif-panel">
                <div className="notif-panel-header">
                  <span style={{ fontWeight: 700 }}>Notifications</span>
                  <button className="btn btn-secondary notif-mark-all" onClick={handleMarkAllRead}>
                    Mark all read
                  </button>
                </div>

                {notifications.length === 0 ? (
                  <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                    No notifications yet
                  </div>
                ) : (
                  <>
                    {notifications.map(n => (
                      <div
                        key={n.id}
                        className={`notif-item ${n.isRead ? '' : 'notif-unread'}`}
                        onClick={() => !n.isRead && handleMarkRead(n.id)}
                      >
                        <div className="notif-icon">{notifIcon(n.type)}</div>
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <div className="notif-title">{n.title}</div>
                          <div className="notif-msg">{n.message}</div>
                          <div className="notif-time">{timeAgo(n.createdAt)}</div>
                        </div>
                        {!n.isRead && <div className="notif-dot" />}
                      </div>
                    ))}
                    {notifMeta.totalPages > 1 && (
                      <div style={{ display: 'flex', justifyContent: 'center', gap: '0.5rem', padding: '0.75rem', borderTop: '1px solid var(--border-color)' }}>
                        <button className="btn btn-secondary" style={{ fontSize: '0.75rem', padding: '0.25rem 0.6rem' }}
                          disabled={notifPage <= 1} onClick={() => loadNotifications(notifPage - 1)}>←</button>
                        <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', alignSelf: 'center' }}>
                          {notifPage} / {notifMeta.totalPages}
                        </span>
                        <button className="btn btn-secondary" style={{ fontSize: '0.75rem', padding: '0.25rem 0.6rem' }}
                          disabled={notifPage >= notifMeta.totalPages} onClick={() => loadNotifications(notifPage + 1)}>→</button>
                      </div>
                    )}
                  </>
                )}
              </div>
            )}
          </div>

          <span className={`badge badge-${user.role.toLowerCase()}`}>{user.role}</span>
          <span style={{ fontSize: '0.9rem', fontWeight: 600 }}>{user.fullName}</span>
          <button onClick={handleLogout} className="btn btn-secondary" style={{ fontSize: '0.8rem', padding: '0.4rem 0.8rem' }}>
            Logout
          </button>
        </div>
      </header>

      <main style={{ flex: 1, padding: '2rem', maxWidth: '1280px', margin: '0 auto', width: '100%' }}>

        <div style={{ display: 'flex', gap: '1rem', marginBottom: '2rem', borderBottom: '1px solid var(--border-color)', paddingBottom: '0.5rem' }}>
          <button className={`btn ${activeTab === 'assignments' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => setActiveTab('assignments')}>Assignments List</button>
          {user.role === 'Admin' && (<>
            <button className={`btn ${activeTab === 'users' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setActiveTab('users')}>User Management</button>
            <button className={`btn ${activeTab === 'classes' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setActiveTab('classes')}>Classes & Subjects</button>
          </>)}
        </div>

        {activeTab === 'assignments' && (
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <div>
                <h3 style={{ fontSize: '1.3rem', fontWeight: 700 }}>Course Assignments</h3>
                <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                  {assignMeta.totalCount} total assignments
                </p>
              </div>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button className="btn btn-secondary" onClick={() => setShowFilters(f => !f)}>
                  🔍 {showFilters ? 'Hide Filters' : 'Filters'}
                </button>
                {(user.role === 'Teacher' || user.role === 'Admin') && (
                  <button className="btn btn-primary" onClick={() => setShowCreateModal(true)}>
                    + Create Assignment
                  </button>
                )}
              </div>
            </div>

            {showFilters && (
              <div className="filter-bar">
                <div className="filter-group">
                  <label>Search</label>
                  <input
                    type="text" className="form-control" placeholder="Title or description..."
                    value={filters.search}
                    onChange={e => setFilters(f => ({ ...f, search: e.target.value }))}
                  />
                </div>
                {user.role !== 'Student' && (
                  <div className="filter-group">
                    <label>Status</label>
                    <select className="form-control" value={filters.isPublished}
                      onChange={e => setFilters(f => ({ ...f, isPublished: e.target.value }))}>
                      <option value="">All</option>
                      <option value="true">Published</option>
                      <option value="false">Draft</option>
                    </select>
                  </div>
                )}
                <div className="filter-group">
                  <label>Sort By</label>
                  <select className="form-control" value={filters.sortBy}
                    onChange={e => setFilters(f => ({ ...f, sortBy: e.target.value }))}>
                    <option value="createdAt">Date Created</option>
                    <option value="deadline">Deadline</option>
                    <option value="title">Title</option>
                  </select>
                </div>
                <div className="filter-group">
                  <label>Order</label>
                  <select className="form-control" value={filters.sortDir}
                    onChange={e => setFilters(f => ({ ...f, sortDir: e.target.value }))}>
                    <option value="desc">Newest First</option>
                    <option value="asc">Oldest First</option>
                  </select>
                </div>
                <button className="btn btn-secondary" style={{ alignSelf: 'flex-end' }}
                  onClick={() => setFilters({ search: '', subjectId: '', isPublished: '', sortBy: 'createdAt', sortDir: 'desc' })}>
                  ✕ Reset
                </button>
              </div>
            )}

            {assignments.length === 0 ? (
              <div style={{ textAlign: 'center', padding: '4rem', color: 'var(--text-muted)' }}>
                <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>📭</div>
                <p>No assignments found.</p>
              </div>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))', gap: '1.5rem' }}>
                {assignments.map((item) => (
                  <div key={item.id} className="glass-card" style={{ display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
                    <div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.75rem' }}>
                        <span className={`badge ${item.isPublished ? 'badge-published' : 'badge-draft'}`}>
                          {item.isPublished ? 'Published' : 'Draft'}
                        </span>
                        <span style={{ fontSize: '0.8rem', color: 'var(--accent-cyan)', fontWeight: 600 }}>
                          Max: {item.maxMarks} pts
                        </span>
                      </div>
                      <h4 style={{ fontSize: '1.1rem', fontWeight: 700, marginBottom: '0.5rem' }}>{item.title}</h4>
                      <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '1rem', minHeight: '40px' }}>
                        {item.description}
                      </p>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: '1rem', display: 'flex', flexWrap: 'wrap', gap: '0.75rem' }}>
                        <span>📚 <strong style={{ color: 'var(--text-primary)' }}>{item.className}</strong></span>
                        <span>📖 <strong style={{ color: 'var(--text-primary)' }}>{item.subjectName}</strong></span>
                        <span>⏰ <strong style={{ color: 'var(--accent-amber)' }}>{new Date(item.deadline).toLocaleString()}</strong></span>
                      </div>
                    </div>
                    <div style={{ paddingTop: '1rem', borderTop: '1px solid var(--border-color)', display: 'flex', gap: '0.5rem', justifyContent: 'space-between', alignItems: 'center' }}>
                      {user.role === 'Student' && (
                        <div>
                          {item.mySubmission ? (
                            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.2rem' }}>
                              <span className="badge badge-graded">
                                {item.mySubmission.status} {item.mySubmission.marks !== null ? `• ${item.mySubmission.marks}/${item.maxMarks}` : ''}
                              </span>
                              {item.mySubmission.feedback && (
                                <p style={{ fontSize: '0.75rem', color: 'var(--accent-emerald)', marginTop: '0.2rem' }}>
                                  "{item.mySubmission.feedback}"
                                </p>
                              )}
                            </div>
                          ) : (
                            <button className="btn btn-primary" style={{ fontSize: '0.8rem' }}
                              onClick={() => setSelectedAssignment(item)}>
                              Submit Solution
                            </button>
                          )}
                        </div>
                      )}
                      {(user.role === 'Teacher' || user.role === 'Admin') && (
                        <div style={{ display: 'flex', gap: '0.5rem', width: '100%', justifyContent: 'space-between' }}>
                          <button className="btn btn-secondary" style={{ fontSize: '0.8rem' }}
                            onClick={() => viewSubmissions(item)}>
                            Submissions ({item.submissionsCount})
                          </button>
                          <button className="btn btn-danger" style={{ fontSize: '0.8rem' }}
                            onClick={() => handleDeleteAssignment(item.id)}>
                            Delete
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}

            {assignMeta.totalPages > 1 && (
              <div className="pagination-bar">
                <button className="btn btn-secondary" disabled={assignPage <= 1}
                  onClick={() => loadAssignments(assignPage - 1)}>← Prev</button>
                <div className="pagination-info">
                  Page <strong>{assignPage}</strong> of <strong>{assignMeta.totalPages}</strong>
                  <span style={{ color: 'var(--text-muted)', fontSize: '0.8rem' }}>({assignMeta.totalCount} total)</span>
                </div>
                <button className="btn btn-secondary" disabled={assignPage >= assignMeta.totalPages}
                  onClick={() => loadAssignments(assignPage + 1)}>Next →</button>
              </div>
            )}
          </div>
        )}

        {activeTab === 'users' && user.role === 'Admin' && (
          <div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: '2rem' }}>
              <div className="glass-panel" style={{ padding: '1.5rem' }}>
                <h4 style={{ fontSize: '1.1rem', fontWeight: 700, marginBottom: '1rem' }}>Add New User</h4>
                <form onSubmit={handleCreateUser}>
                  <div className="form-group">
                    <label>Full Name</label>
                    <input type="text" className="form-control" value={newUser.fullName}
                      onChange={e => setNewUser({ ...newUser, fullName: e.target.value })} required />
                  </div>
                  <div className="form-group">
                    <label>Email Address</label>
                    <input type="email" className="form-control" value={newUser.email}
                      onChange={e => setNewUser({ ...newUser, email: e.target.value })} required />
                  </div>
                  <div className="form-group">
                    <label>Role</label>
                    <select className="form-control" value={newUser.role}
                      onChange={e => setNewUser({ ...newUser, role: e.target.value })}>
                      <option value="Student">Student</option>
                      <option value="Teacher">Teacher</option>
                      <option value="Admin">Admin</option>
                    </select>
                  </div>
                  {newUser.role === 'Student' && (
                    <div className="form-group">
                      <label>Class/Course</label>
                      <select className="form-control" value={newUser.classId}
                        onChange={e => setNewUser({ ...newUser, classId: e.target.value })}>
                        <option value="">Select Class...</option>
                        {classesList.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                      </select>
                    </div>
                  )}
                  <button type="submit" className="btn btn-primary" style={{ width: '100%', marginTop: '0.5rem' }}>Create User</button>
                </form>
              </div>
              <div className="glass-panel" style={{ padding: '1.5rem' }}>
                <h4 style={{ fontSize: '1.1rem', fontWeight: 700, marginBottom: '1rem' }}>All Users ({usersList.length})</h4>
                <table className="data-table">
                  <thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Class</th><th>Action</th></tr></thead>
                  <tbody>
                    {usersList.map(u => (
                      <tr key={u.id}>
                        <td><strong>{u.fullName}</strong></td>
                        <td>{u.email}</td>
                        <td><span className={`badge badge-${u.role.toLowerCase()}`}>{u.role}</span></td>
                        <td>{u.className || '-'}</td>
                        <td>
                          <button className="btn btn-danger" style={{ padding: '0.2rem 0.5rem', fontSize: '0.75rem' }}
                            onClick={() => handleDeleteUser(u.id)}>Delete</button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )}

        {activeTab === 'classes' && user.role === 'Admin' && (
          <div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '2rem' }}>
              <div className="glass-panel" style={{ padding: '1.5rem' }}>
                <h4 style={{ fontSize: '1.1rem', fontWeight: 700, marginBottom: '1rem' }}>Create Class / Course</h4>
                <form onSubmit={handleCreateClass}>
                  <div className="form-group">
                    <label>Class Name</label>
                    <input type="text" className="form-control" placeholder="e.g. Class 12 - Science"
                      value={newClass.name} onChange={e => setNewClass({ ...newClass, name: e.target.value })} required />
                  </div>
                  <div className="form-group">
                    <label>Class Code</label>
                    <input type="text" className="form-control" placeholder="e.g. 12-SCI"
                      value={newClass.code} onChange={e => setNewClass({ ...newClass, code: e.target.value })} required />
                  </div>
                  <button type="submit" className="btn btn-primary" style={{ width: '100%' }}>Add Class</button>
                </form>
              </div>
              <div className="glass-panel" style={{ padding: '1.5rem' }}>
                <h4 style={{ fontSize: '1.1rem', fontWeight: 700, marginBottom: '1rem' }}>Add Subject & Assign Teacher</h4>
                <form onSubmit={handleCreateSubject}>
                  <div className="form-group">
                    <label>Subject Name</label>
                    <input type="text" className="form-control" placeholder="e.g. Biology"
                      value={newSubject.name} onChange={e => setNewSubject({ ...newSubject, name: e.target.value })} required />
                  </div>
                  <div className="form-group">
                    <label>Class</label>
                    <select className="form-control" value={newSubject.classId}
                      onChange={e => setNewSubject({ ...newSubject, classId: e.target.value })} required>
                      <option value="">Select Class...</option>
                      {classesList.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                    </select>
                  </div>
                  <div className="form-group">
                    <label>Assigned Teacher</label>
                    <select className="form-control" value={newSubject.teacherId}
                      onChange={e => setNewSubject({ ...newSubject, teacherId: e.target.value })}>
                      <option value="">Select Teacher...</option>
                      {usersList.filter(u => u.role === 'Teacher').map(t => (
                        <option key={t.id} value={t.id}>{t.fullName}</option>
                      ))}
                    </select>
                  </div>
                  <button type="submit" className="btn btn-primary" style={{ width: '100%' }}>Add Subject</button>
                </form>
              </div>
            </div>
          </div>
        )}
      </main>

      {showCreateModal && (
        <div className="modal-overlay">
          <div className="modal-container">
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '1.5rem' }}>Create Assignment</h3>
            <form onSubmit={handleCreateAssignment}>
              <div className="form-group"><label>Title</label>
                <input type="text" className="form-control" value={newAssignment.title}
                  onChange={e => setNewAssignment({ ...newAssignment, title: e.target.value })} required /></div>
              <div className="form-group"><label>Description / Instructions</label>
                <textarea className="form-control" value={newAssignment.description}
                  onChange={e => setNewAssignment({ ...newAssignment, description: e.target.value })} /></div>
              <div className="form-group"><label>Class</label>
                <select className="form-control" value={newAssignment.classId}
                  onChange={e => setNewAssignment({ ...newAssignment, classId: e.target.value })} required>
                  <option value="">Select Class...</option>
                  {classesList.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select></div>
              <div className="form-group"><label>Subject</label>
                <select className="form-control" value={newAssignment.subjectId}
                  onChange={e => setNewAssignment({ ...newAssignment, subjectId: e.target.value })} required>
                  <option value="">Select Subject...</option>
                  {classesList.find(c => c.id === parseInt(newAssignment.classId))?.subjects.map((s: any) => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </select></div>
              <div className="form-group"><label>Deadline</label>
                <input type="datetime-local" className="form-control" value={newAssignment.deadline}
                  onChange={e => setNewAssignment({ ...newAssignment, deadline: e.target.value })} required /></div>
              <div className="form-group"><label>Maximum Marks</label>
                <input type="number" className="form-control" value={newAssignment.maxMarks}
                  onChange={e => setNewAssignment({ ...newAssignment, maxMarks: parseInt(e.target.value) })} required /></div>
              <div className="form-group" style={{ flexDirection: 'row', alignItems: 'center', gap: '0.5rem' }}>
                <input type="checkbox" id="publishCheck" checked={newAssignment.isPublished}
                  onChange={e => setNewAssignment({ ...newAssignment, isPublished: e.target.checked })} />
                <label htmlFor="publishCheck" style={{ cursor: 'pointer' }}>Publish immediately for students</label>
              </div>
              <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button type="button" className="btn btn-secondary" style={{ flex: 1 }}
                  onClick={() => setShowCreateModal(false)}>Cancel</button>
                <button type="submit" className="btn btn-primary" style={{ flex: 1 }}>Publish / Save</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {selectedAssignment && user.role === 'Student' && (
        <div className="modal-overlay">
          <div className="modal-container">
            <h3 style={{ fontSize: '1.25rem', fontWeight: 700, marginBottom: '0.5rem' }}>
              Submit: {selectedAssignment.title}
            </h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '1.5rem' }}>
              Max Marks: <strong>{selectedAssignment.maxMarks}</strong> • Deadline: <strong>{new Date(selectedAssignment.deadline).toLocaleString()}</strong>
            </p>
            <form onSubmit={handleStudentSubmit}>
              <div className="form-group"><label>Text Response / Answer</label>
                <textarea className="form-control" placeholder="Write your solution here..."
                  value={submitContent} onChange={e => setSubmitContent(e.target.value)} /></div>
              <div className="form-group"><label>External URL (GitHub / Google Docs)</label>
                <input type="url" className="form-control" placeholder="https://github.com/..."
                  value={submitLink} onChange={e => setSubmitLink(e.target.value)} /></div>
              <div className="form-group"><label>Attach File / Document</label>
                <input type="file" className="form-control"
                  onChange={e => setSelectedFile(e.target.files ? e.target.files[0] : null)} /></div>
              <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1.5rem' }}>
                <button type="button" className="btn btn-secondary" style={{ flex: 1 }}
                  onClick={() => setSelectedAssignment(null)}>Cancel</button>
                <button type="submit" className="btn btn-primary" style={{ flex: 1 }}>Submit Assignment</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showSubmissionsModal && selectedAssignment && (
        <div className="modal-overlay">
          <div className="modal-container" style={{ maxWidth: '780px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h3 style={{ fontSize: '1.25rem', fontWeight: 700 }}>Submissions: {selectedAssignment.title}</h3>
              <button className="btn btn-secondary" onClick={() => setShowSubmissionsModal(false)}>Close</button>
            </div>

            <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem' }}>
              {['', 'Submitted', 'Graded', 'NeedsRevision'].map(s => (
                <button key={s} className={`btn ${subStatusFilter === s ? 'btn-primary' : 'btn-secondary'}`}
                  style={{ fontSize: '0.78rem', padding: '0.3rem 0.7rem' }}
                  onClick={() => { setSubStatusFilter(s); viewSubmissions(selectedAssignment, 1); }}>
                  {s || 'All'}
                </button>
              ))}
            </div>

            {submissionsList.length === 0 ? (
              <p style={{ color: 'var(--text-secondary)', padding: '2rem 0', textAlign: 'center' }}>
                No submissions received yet.
              </p>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                {submissionsList.map(sub => (
                  <div key={sub.id} className="glass-card" style={{ padding: '1rem' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                      <div>
                        <strong>{sub.studentName}</strong> ({sub.studentEmail})
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                          Submitted: {new Date(sub.submittedAt).toLocaleString()}
                        </div>
                      </div>
                      <span className="badge badge-graded">
                        {sub.status}: {sub.marks !== null ? `${sub.marks}/${selectedAssignment.maxMarks}` : 'Ungraded'}
                      </span>
                    </div>
                    {sub.content && (
                      <div style={{ background: 'var(--bg-input)', padding: '0.75rem', borderRadius: '6px', fontSize: '0.85rem', marginBottom: '0.5rem' }}>
                        <strong>Answer:</strong> {sub.content}
                      </div>
                    )}
                    {sub.linkUrl && (
                      <div style={{ fontSize: '0.85rem', marginBottom: '0.5rem' }}>
                        🔗 <a href={sub.linkUrl} target="_blank" rel="noreferrer" style={{ color: 'var(--accent-cyan)', textDecoration: 'underline' }}>{sub.linkUrl}</a>
                      </div>
                    )}
                    {sub.filePath && (
                      <div style={{ fontSize: '0.85rem', marginBottom: '0.5rem' }}>
                        📁 <a href={sub.filePath} target="_blank" rel="noreferrer" style={{ color: 'var(--accent-emerald)', textDecoration: 'underline' }}>Download Attachment</a>
                      </div>
                    )}
                    {sub.feedback && (
                      <div style={{ fontSize: '0.8rem', color: 'var(--accent-amber)' }}>Feedback: "{sub.feedback}"</div>
                    )}
                    <div style={{ marginTop: '0.75rem', textAlign: 'right' }}>
                      <button className="btn btn-primary" style={{ fontSize: '0.75rem', padding: '0.35rem 0.75rem' }}
                        onClick={() => { setGradingSubmission(sub); setGradeMarks(sub.marks || 0); setGradeFeedback(sub.feedback || ''); }}>
                        Grade / Feedback
                      </button>
                    </div>
                  </div>
                ))}

                {subMeta.totalPages > 1 && (
                  <div className="pagination-bar" style={{ marginTop: '0.5rem' }}>
                    <button className="btn btn-secondary" disabled={subPage <= 1}
                      onClick={() => viewSubmissions(selectedAssignment, subPage - 1)}>← Prev</button>
                    <span className="pagination-info">Page {subPage} / {subMeta.totalPages}</span>
                    <button className="btn btn-secondary" disabled={subPage >= subMeta.totalPages}
                      onClick={() => viewSubmissions(selectedAssignment, subPage + 1)}>Next →</button>
                  </div>
                )}
              </div>
            )}

            {gradingSubmission && (
              <div style={{ marginTop: '1.5rem', paddingTop: '1.5rem', borderTop: '2px solid var(--accent-primary)' }}>
                <h4 style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '1rem' }}>
                  Grade: {gradingSubmission.studentName}
                </h4>
                <form onSubmit={handleSaveGrade}>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                    <div className="form-group"><label>Marks (Max {selectedAssignment.maxMarks})</label>
                      <input type="number" className="form-control" max={selectedAssignment.maxMarks}
                        value={gradeMarks} onChange={e => setGradeMarks(parseFloat(e.target.value))} required /></div>
                    <div className="form-group"><label>Status</label>
                      <select className="form-control" value={gradeStatus} onChange={e => setGradeStatus(e.target.value)}>
                        <option value="Graded">Graded</option>
                        <option value="NeedsRevision">Needs Revision</option>
                      </select></div>
                  </div>
                  <div className="form-group"><label>Feedback</label>
                    <textarea className="form-control" value={gradeFeedback}
                      onChange={e => setGradeFeedback(e.target.value)}
                      placeholder="Feedback comments..." /></div>
                  <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                    <button type="button" className="btn btn-secondary" onClick={() => setGradingSubmission(null)}>Cancel</button>
                    <button type="submit" className="btn btn-primary">Save Grade & Feedback</button>
                  </div>
                </form>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
