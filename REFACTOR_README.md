# Raytha Pages/ Refactor Documentation

**Comprehensive refactor plan for scalability, maintainability, and consistency**

---

## 📚 Documentation Index

This refactor consists of multiple detailed documents. Start here to navigate to the right resource:

### 1. **[REFACTOR_PLAN_SUMMARY.md](./REFACTOR_PLAN_SUMMARY.md)** ⭐ **START HERE**
   - **5-minute read**
   - Executive summary of goals, changes, and timeline
   - Before/after comparisons
   - Risk assessment
   - Perfect for stakeholders and quick overview

### 2. **[REFACTOR_PLAN.md](./REFACTOR_PLAN.md)** 📋 **COMPLETE SPECIFICATION**
   - **60+ pages, 30-minute read**
   - Exhaustive technical specification
   - Every pattern, component, and decision documented
   - 15-17 PR migration plan with checklists
   - Testing strategy and CI enforcement
   - Reference document during implementation

### 3. **[docs/refactor-architecture.md](./docs/refactor-architecture.md)** 🏗️ **ARCHITECTURE DIAGRAMS**
   - **Visual guide, 15-minute read**
   - Before/after architecture diagrams
   - Navigation system evolution
   - Routing flow diagrams
   - TagHelper and ViewComponent patterns illustrated
   - Perfect for visual learners

### 4. **[docs/refactor-examples.md](./docs/refactor-examples.md)** 💻 **CODE EXAMPLES**
   - **Practical guide, 20-minute read**
   - Concrete before/after code samples
   - 5 common page patterns with full code
   - Copy-paste examples for implementation
   - Pitfalls and solutions
   - PR review checklist
   - Essential for developers doing the refactor

---

## 🎯 Quick Start Guide

### For Project Managers / Stakeholders

1. Read **[REFACTOR_PLAN_SUMMARY.md](./REFACTOR_PLAN_SUMMARY.md)** (5 min)
2. Review timeline and risk assessment
3. Approve approach and resources
4. Track progress via GitHub Project board (to be created)

### For Architects / Tech Leads

1. Read **[REFACTOR_PLAN_SUMMARY.md](./REFACTOR_PLAN_SUMMARY.md)** (5 min)
2. Deep dive: **[REFACTOR_PLAN.md](./REFACTOR_PLAN.md)** (30 min)
3. Review **[docs/refactor-architecture.md](./docs/refactor-architecture.md)** (15 min)
4. Provide feedback on approach and patterns
5. Help establish code review standards

### For Developers Implementing Changes

1. Skim **[REFACTOR_PLAN_SUMMARY.md](./REFACTOR_PLAN_SUMMARY.md)** (5 min)
2. Study **[docs/refactor-examples.md](./docs/refactor-examples.md)** (20 min) ⭐
3. Reference **[REFACTOR_PLAN.md](./REFACTOR_PLAN.md)** for specific sections as needed
4. Follow PR checklist in examples doc
5. Run CI checks before submitting PR

### For Code Reviewers

1. Review **[docs/refactor-examples.md](./docs/refactor-examples.md)** - "PR Checklist" section
2. Reference **[REFACTOR_PLAN.md](./REFACTOR_PLAN.md)** - "Coding Standards" section
3. Verify CI checks pass
4. Ensure patterns are consistent with examples

---

## 🎓 Key Concepts (TL;DR)

### The Big Picture

**What we're doing:**
- Removing all Hotwire/Stimulus code
- Centralizing routing via `RouteNames` constants
- Creating reusable navigation with active state + breadcrumbs
- Consolidating duplicate layouts and partials
- Establishing consistent patterns for all features

**Why:**
- 70% reduction in duplicated code
- Easier to maintain and extend
- Consistent UX across all features
- Modern, clean architecture
- Scalable foundation for future growth

### Core Patterns

```
RouteNames.cs → Single source of truth for all routes
NavMap.cs → Navigation structure metadata
BasePageModel → Common functionality for all pages
TagHelpers → Reusable UI logic (alerts, breadcrumbs, active state)
ViewComponents → Data-driven widgets (sidebar, toolbar)
Partials → Pure markup reuse (empty states, pagination)
SubActionLayout → Shared layout for detail/edit pages
```

### Before → After

| Aspect | Before | After |
|--------|--------|-------|
| **Routes** | Hardcoded strings | `RouteNames.Users.Index` |
| **Navigation** | Inline in layout | `NavMap` + ViewComponent |
| **Active state** | Manual checks | `nav-active-section` TagHelper |
| **Breadcrumbs** | None | Auto-generated or manual |
| **Alerts** | Inline `@if` checks | `<alert />` TagHelper |
| **SubActionLayout** | 4 duplicates | 1 shared |
| **JavaScript** | 30+ Stimulus controllers | Bootstrap native |

---

## 📊 Project Status

**Current Phase:** ✅ Planning Complete

**Next Steps:**
1. Team review and feedback (Est. 2-3 days)
2. Create GitHub Project board for PR tracking
3. Begin implementation with PR0 (Foundation)

**Estimated Timeline:**
- **Option A:** 4-6 weeks (1 developer full-time)
- **Option B:** 8-12 weeks (0.5 FTE)

**Progress Tracking:**
- [ ] PR0: Foundation (RouteNames, TagHelpers, models)
- [ ] PR1: Shared layouts & partials
- [ ] PR2: Sidebar ViewComponent
- [ ] PR3-4: Dashboard & Login
- [ ] PR5: Users (establishes pattern)
- [ ] PR6-13: Remaining features (one PR each)
- [ ] PR14: ContentTypes & ContentItems
- [ ] PR15: Themes & RaythaFunctions
- [ ] PR16: Remove Stimulus JS
- [ ] PR17: Final cleanup & docs

---

## 🛠️ Tools & Scripts

### CI Enforcement Scripts

**`scripts/check-routes.sh`**
```bash
# Fails if hardcoded routes found
./scripts/check-routes.sh
```

**`scripts/check-stimulus.sh`**
```bash
# Fails if Stimulus attributes found
./scripts/check-stimulus.sh
```

**`scripts/check-route-coverage.sh`**
```bash
# Fails if pages missing from RouteNames
./scripts/check-route-coverage.sh
```

### Developer Helpers

**Find hardcoded routes:**
```bash
grep -r 'href="/' --include="*.cshtml" src/Raytha.Web/Areas/Admin/Pages
grep -r 'asp-page="/[^@]' --include="*.cshtml" src/Raytha.Web/Areas/Admin/Pages
```

**Find Stimulus attributes:**
```bash
grep -r 'data-controller\|data-action\|data-turbo' src/Raytha.Web --include="*.cshtml"
```

**Find inline alert checks (should use TagHelper):**
```bash
grep -r 'ViewData\["ErrorMessage"\]' --include="*.cshtml" src/Raytha.Web/Areas/Admin/Pages
```

---

## 🤝 Contributing

### PR Guidelines

1. **One feature per PR** - Don't mix Users + Admins refactor in same PR
2. **Follow the examples** - Use patterns from `docs/refactor-examples.md`
3. **Test thoroughly** - All CRUD operations, breadcrumbs, active states
4. **Run CI checks locally** - Before pushing
5. **Update tests** - If adding new routes or components
6. **Request review** - From tech lead or senior developer

### Code Review Standards

**Must Have:**
- ✅ All CI checks pass
- ✅ No hardcoded routes
- ✅ No Stimulus attributes
- ✅ Breadcrumbs on all pages
- ✅ Active menu state correct
- ✅ Tests added/updated

**Should Have:**
- ✅ Consistent with example patterns
- ✅ No inline alert checks (use `<alert />`)
- ✅ Uses shared partials where applicable
- ✅ Proper layout inheritance

### Getting Help

**Questions about:**
- **Architecture** → Read `docs/refactor-architecture.md`
- **Specific pattern** → Read `docs/refactor-examples.md`
- **Why we're doing this** → Read `REFACTOR_PLAN_SUMMARY.md`
- **Detailed spec** → Read `REFACTOR_PLAN.md`

**Still stuck?**
- Ask in #raytha-refactor Slack channel (to be created)
- Tag @tech-lead in PR comments
- Schedule pair programming session

---

## 📝 Decision Log

### Why Remove Stimulus?

**Decision:** Remove all Stimulus/Hotwire code

**Rationale:**
- Not being used consistently
- Adds complexity and bundle size
- Bootstrap 5 native components are sufficient
- Vanilla JS for edge cases is simpler
- Reduces dependencies

**Alternatives Considered:**
- Keep Stimulus → Rejected (adds weight, inconsistent usage)
- Replace with Alpine.js → Rejected (still adds dependency)
- Pure vanilla JS → Chosen (minimal, no dependencies)

### Why Centralize Routes?

**Decision:** All routes via `RouteNames` constants

**Rationale:**
- Compile-time safety (typos caught early)
- Refactoring friendly (change route in one place)
- IDE autocomplete support
- Easier to audit (grep for violations)
- CI enforcement possible

**Alternatives Considered:**
- Keep hardcoded strings → Rejected (brittle, error-prone)
- Use route attributes only → Rejected (still allows hardcoding)

### Why ViewComponent for Sidebar?

**Decision:** Extract sidebar nav to ViewComponent

**Rationale:**
- Testable (unit and integration tests)
- Reusable (can be used in other layouts)
- Cleaner layouts (200 lines → 1 line)
- Centralizes permission logic
- Easier to maintain

**Alternatives Considered:**
- Keep inline in layout → Rejected (not testable, duplicated logic)
- Use TagHelper → Rejected (TagHelpers best for element enhancement, not large widgets)
- Use Partial → Rejected (can't inject services for permission checks)

---

## 🧪 Testing Strategy

### Unit Tests

- RouteNames constants (all non-null)
- NavMap items (all have valid routes)
- TagHelpers (active state, breadcrumbs, alerts)
- Base PageModel helpers

### Integration Tests

- ViewComponents (sidebar, toolbar)
- Navigation structure (authorized items only)
- Breadcrumb generation

### Manual Testing Per Feature

- [ ] All CRUD operations work
- [ ] Active menu state correct
- [ ] Breadcrumbs display on all pages
- [ ] Back navigation works (SubActionLayout)
- [ ] Alerts display correctly
- [ ] Empty states show when appropriate
- [ ] Pagination works
- [ ] Search works
- [ ] No console errors
- [ ] Mobile responsive

### CI Checks (Automated)

- ✅ No hardcoded routes
- ✅ No Stimulus attributes
- ✅ All routes have RouteNames entries
- ✅ View compilation passes
- ✅ Tests pass

---

## 📈 Success Metrics

### Quantitative

- [ ] 70%+ reduction in duplicated markup
- [ ] Zero hardcoded routes (CI enforced)
- [ ] Zero Stimulus attributes (CI enforced)
- [ ] Breadcrumbs on 100% of pages
- [ ] No performance regressions (±5%)

### Qualitative

- [ ] Developers find it easier to add new features
- [ ] Code reviews are faster (consistent patterns)
- [ ] New team members onboard quicker
- [ ] UX is more consistent across features

### Before/After Comparison

**File Counts:**
| Type | Before | After | Change |
|------|--------|-------|--------|
| Layout files | 6 | 5 | -1 |
| Shared partials | 12 | 16 | +4 |
| ViewComponents | 0 | 3 | +3 |
| TagHelpers | 2 | 6 | +4 |
| Stimulus controllers | 30 | 0 | -30 ✅ |
| Feature pages | ~150 | ~150 | 0 |

**Code Quality:**
- Duplicated SubActionLayout: 4 → 1 (-75%)
- Hardcoded routes: ~50 → 0 (-100%)
- Inline nav logic: 200 lines → ViewComponent
- Breadcrumbs: 0 pages → All pages

---

## 🔗 External References

- [ASP.NET Core Razor Pages Documentation](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/)
- [Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.0/)
- [Raytha Instructions](./.github/instructions/raytha.instructions.md)

---

## 📞 Contact

**Project Owner:** [Name]  
**Tech Lead:** [Name]  
**Slack Channel:** #raytha-refactor (to be created)  
**GitHub Project:** [Link] (to be created)

---

## ✅ Next Actions

**For the Team (Today):**
1. [ ] Review this README
2. [ ] Read REFACTOR_PLAN_SUMMARY.md
3. [ ] Provide feedback via Slack/email
4. [ ] Attend kickoff meeting (schedule TBD)

**For Project Owner (This Week):**
1. [ ] Approve plan and timeline
2. [ ] Allocate developer resources
3. [ ] Create GitHub Project board
4. [ ] Schedule kickoff meeting

**For Tech Lead (This Week):**
1. [ ] Address team feedback
2. [ ] Set up CI checks (scripts)
3. [ ] Create PR templates
4. [ ] Establish code review process

**For Developer(s) (Next Week):**
1. [ ] Study docs/refactor-examples.md
2. [ ] Set up local environment
3. [ ] Begin PR0 (Foundation)

---

**Last Updated:** November 2, 2025  
**Version:** 1.0 (Initial Plan)

---

**Let's build something great! 🚀**

